using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.MicrosoftTeams;
using AiEngineeringManagerCopilot.Application.Slack;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using AiEngineeringManagerCopilot.IntegrationTests.Helpers;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.MicrosoftTeams;

public sealed class MicrosoftTeamsWebhookEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string WebhookUrl =
        "https://test.environment.api.powerplatform.com/powerautomate/automations/direct/workflows/test/triggers/manual/paths/invoke?api-version=1&sig=fake-test-signature";

    private readonly HttpClient _client = factory.CreateClient();
    private readonly FakeMicrosoftTeamsWebhookClient _teamsClient =
        factory.Services.GetRequiredService<FakeMicrosoftTeamsWebhookClient>();
    private readonly FakeSlackWebhookClient _slackClient =
        factory.Services.GetRequiredService<FakeSlackWebhookClient>();

    private async Task<TeamResponse> CreateTeamAsync()
    {
        _teamsClient.Reset();
        _slackClient.Reset();
        var response = await _client.PostAsJsonAsync(
            "/teams", new CreateTeamRequest($"MicrosoftTeams-{Guid.NewGuid():N}", null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadApiJsonAsync<TeamResponse>())!;
    }

    private async Task ConnectAsync(Guid teamId)
    {
        var response = await _client.PostAsJsonAsync(
            $"/teams/{teamId}/microsoft-teams", new CreateMicrosoftTeamsWebhookRequest(WebhookUrl));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateAndGet_ShouldProtectWebhookSecret()
    {
        var team = await CreateTeamAsync();
        var create = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/microsoft-teams", new CreateMicrosoftTeamsWebhookRequest(WebhookUrl));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        create.Headers.Location!.ToString().Should().Be($"/teams/{team.Id}/microsoft-teams");
        (await create.Content.ReadAsStringAsync()).Should().NotContain("fake-test-signature");

        var get = await _client.GetAsync($"/teams/{team.Id}/microsoft-teams");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        (await get.Content.ReadAsStringAsync()).Should().NotContain("webhookUrl");
        var result = await get.Content.ReadApiJsonAsync<MicrosoftTeamsWebhookConnectionResponse>();
        result!.TeamId.Should().Be(team.Id);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connection = await db.MicrosoftTeamsWebhookConnections.SingleAsync(x => x.TeamId == team.Id);
        connection.WebhookUrlEncrypted.Should().NotContain("fake-test-signature");
        connection.WebhookUrlEncrypted.Should().NotBe(WebhookUrl);
    }

    [Theory]
    [InlineData("https://example.com/workflows/test/triggers/manual/paths/invoke?sig=fake")]
    [InlineData("http://test.logic.azure.com/workflows/test/triggers/manual/paths/invoke?sig=fake")]
    [InlineData("https://test.logic.azure.com.evil.example/workflows/test/triggers/manual/paths/invoke?sig=fake")]
    [InlineData("https://user@test.logic.azure.com/workflows/test/triggers/manual/paths/invoke?sig=fake")]
    [InlineData("https://test.logic.azure.com/workflows/test/triggers/manual/paths/invoke")]
    public async Task Create_WithInvalidWebhook_ShouldReturnBadRequest(string url)
    {
        var team = await CreateTeamAsync();
        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/microsoft-teams", new CreateMicrosoftTeamsWebhookRequest(url));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _teamsClient.LastWebhookUri.Should().BeNull();
    }

    [Fact]
    public async Task Create_WhenAlreadyConfigured_ShouldReturnConflict()
    {
        var team = await CreateTeamAsync();
        await ConnectAsync(team.Id);
        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/microsoft-teams", new CreateMicrosoftTeamsWebhookRequest(WebhookUrl));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Test_ShouldSendToConfiguredWebhook()
    {
        var team = await CreateTeamAsync();
        await ConnectAsync(team.Id);
        var response = await _client.PostAsync($"/teams/{team.Id}/microsoft-teams/test", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadApiJsonAsync<TestMicrosoftTeamsWebhookResponse>();
        result!.Success.Should().BeTrue();
        _teamsClient.LastWebhookUri!.AbsoluteUri.Should().Be(WebhookUrl);
        _teamsClient.LastMessage.Should().Contain("test successful");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Test_WhenDeliveryFails_ShouldReturnFailure(bool networkFailure)
    {
        var team = await CreateTeamAsync();
        await ConnectAsync(team.Id);
        _teamsClient.Result = false;
        if (networkFailure)
        {
            _teamsClient.Failure = new HttpRequestException("Teams unavailable.");
        }

        var response = await _client.PostAsync($"/teams/{team.Id}/microsoft-teams/test", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadApiJsonAsync<TestMicrosoftTeamsWebhookResponse>();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldRemoveOnlyTeamsConnection()
    {
        var team = await CreateTeamAsync();
        await ConnectAsync(team.Id);
        var slack = await _client.PostAsJsonAsync($"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest("https://hooks.slack.com/services/T000/B000/fake-token"));
        slack.StatusCode.Should().Be(HttpStatusCode.Created);

        (await _client.DeleteAsync($"/teams/{team.Id}/microsoft-teams"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/teams/{team.Id}/microsoft-teams"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/teams/{team.Id}/slack"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Endpoints_ForAnotherUsersTeam_ShouldReturnNotFound()
    {
        var team = await CreateTeamAsync();
        await ConnectAsync(team.Id);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var otherUser = new User
        {
            Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.com",
            Name = "Other owner", CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(otherUser);
        var entity = await db.Teams.SingleAsync(x => x.Id == team.Id);
        entity.OwnerUserId = otherUser.Id;
        await db.SaveChangesAsync();

        (await _client.GetAsync($"/teams/{team.Id}/microsoft-teams"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.PostAsync($"/teams/{team.Id}/microsoft-teams/test", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.DeleteAsync($"/teams/{team.Id}/microsoft-teams"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.PostAsJsonAsync($"/teams/{team.Id}/microsoft-teams",
            new CreateMicrosoftTeamsWebhookRequest(WebhookUrl)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        _teamsClient.LastWebhookUri.Should().BeNull();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GenerateReport_ShouldNotifyBothIntegrationsDespiteDeliveryFailure(
        bool slackFails, bool teamsFails)
    {
        var team = await CreateTeamAsync();
        await ConnectAsync(team.Id);
        var slack = await _client.PostAsJsonAsync($"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest("https://hooks.slack.com/services/T000/B000/fake-token"));
        slack.StatusCode.Should().Be(HttpStatusCode.Created);
        if (slackFails)
        {
            _slackClient.Failure = new HttpRequestException("Slack unavailable.");
        }
        if (teamsFails)
        {
            _teamsClient.Failure = new HttpRequestException("Teams unavailable.");
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.EngineeringMetrics.AddRange(Enum.GetValues<MetricType>().Select(metric =>
            new EngineeringMetric
            {
                Id = Guid.NewGuid(), TeamId = team.Id, MetricType = metric, Value = 5,
                DataStatus = MetricDataStatus.Available,
                PeriodStart = new DateOnly(2026, 9, 1), PeriodEnd = new DateOnly(2026, 9, 30),
                CreatedAt = DateTimeOffset.UtcNow
            }));
        await db.SaveChangesAsync();

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/reports?periodStart=2026-09-01&periodEnd=2026-09-30", null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _teamsClient.LastWebhookUri!.AbsoluteUri.Should().Be(WebhookUrl);
        _teamsClient.LastMessage.Should().Contain(team.Name).And.Contain("2026-09-01")
            .And.Contain("Health score");
        if (!slackFails)
        {
            _slackClient.LastMessage.Should().Contain(team.Name);
        }
    }
}
