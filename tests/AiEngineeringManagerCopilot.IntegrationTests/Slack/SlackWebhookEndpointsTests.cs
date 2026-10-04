using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Slack;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using AiEngineeringManagerCopilot.IntegrationTests.Helpers;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;

namespace AiEngineeringManagerCopilot.IntegrationTests.Slack;

public sealed class SlackWebhookEndpointsTests(
    CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string WebhookUrl =
        "https://hooks.slack.com/services/T000/B000/fake-test-token";

    private readonly HttpClient _client = factory.CreateClient();
    private readonly FakeSlackWebhookClient _slackClient =
        factory.Services.GetRequiredService<FakeSlackWebhookClient>();

    private async Task<TeamResponse> CreateTeamAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/teams",
            new CreateTeamRequest($"Slack-{Guid.NewGuid():N}", null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadApiJsonAsync<TeamResponse>())!;
    }

    [Fact]
    public async Task CreateAndGet_ShouldNotExposeWebhookSecret()
    {
        var team = await CreateTeamAsync();
        var create = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest(WebhookUrl));

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        (await create.Content.ReadAsStringAsync()).Should().NotContain(WebhookUrl);

        var get = await _client.GetAsync($"/teams/{team.Id}/slack");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await get.Content.ReadAsStringAsync();
        body.Should().NotContain(WebhookUrl);
        body.Should().NotContain("WebhookUrl");
    }

    [Theory]
    [InlineData("https://example.com/services/T000/B000/token")]
    [InlineData("http://hooks.slack.com/services/T000/B000/token")]
    [InlineData("https://hooks.slack.com.evil.example/services/T000/B000/token")]
    [InlineData("https://user@hooks.slack.com/services/T000/B000/token")]
    public async Task Create_WithNonSlackWebhook_ShouldReturnBadRequest(string url)
    {
        var team = await CreateTeamAsync();
        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest(url));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _slackClient.LastWebhookUri.Should().BeNull();
    }

    [Fact]
    public async Task Create_WhenAlreadyConfigured_ShouldReturnConflict()
    {
        var team = await CreateTeamAsync();
        var first = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest(WebhookUrl));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest(WebhookUrl));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Test_ShouldSendToConfiguredSlackWebhook()
    {
        _slackClient.Reset();
        var team = await CreateTeamAsync();
        var create = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest(WebhookUrl));
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.PostAsync($"/teams/{team.Id}/slack/test", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadApiJsonAsync<TestSlackWebhookResponse>();

        result!.Success.Should().BeTrue();
        _slackClient.LastWebhookUri!.AbsoluteUri.Should().Be(WebhookUrl);
        _slackClient.LastMessage.Should().Contain("test successful");
    }

    [Fact]
    public async Task Delete_ShouldRemoveSlackWebhook()
    {
        var team = await CreateTeamAsync();
        var create = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest(WebhookUrl));
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var delete = await _client.DeleteAsync($"/teams/{team.Id}/slack");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/teams/{team.Id}/slack"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateReport_ShouldNotifyConfiguredSlackWebhook()
    {
        _slackClient.Reset();
        var team = await CreateTeamAsync();
        var connection = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest(WebhookUrl));
        connection.StatusCode.Should().Be(HttpStatusCode.Created);

        await SeedAllMetricsAsync(team.Id);
        var response = await _client.PostAsync(
            $"/teams/{team.Id}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _slackClient.LastWebhookUri!.AbsoluteUri.Should().Be(WebhookUrl);
        _slackClient.LastMessage.Should().Contain(team.Name);
        _slackClient.LastMessage.Should().Contain("2026-09-01");
        _slackClient.LastMessage.Should().Contain("Health score");
        var report = await response.Content.ReadApiJsonAsync<AiEngineeringManagerCopilot.Application.Reports.EngineeringReportResponse>();
        _slackClient.LastReport.Should().NotBeNull();
        _slackClient.LastReport!.ReportUrl.AbsoluteUri.Should()
            .Be($"http://localhost:4200/reports/{report!.Id}?teamId={team.Id}");
        _slackClient.LastReport.ExecutiveSummary.Should().Be(report.ExecutiveSummary);
    }

    [Fact]
    public async Task GenerateReport_ShouldSucceedWhenSlackDeliveryFails()
    {
        _slackClient.Reset();
        _slackClient.Failure = new HttpRequestException("Slack is unavailable.");
        var team = await CreateTeamAsync();
        var connection = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/slack",
            new CreateSlackWebhookRequest(WebhookUrl));
        connection.StatusCode.Should().Be(HttpStatusCode.Created);
        await SeedAllMetricsAsync(team.Id);

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task SeedAllMetricsAsync(Guid teamId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var values = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 8,
            [MetricType.PRReviewTime] = 4,
            [MetricType.DeploymentFrequency] = 20,
            [MetricType.ChangeFailureRate] = 5,
            [MetricType.LeadTime] = 24,
            [MetricType.OpenPRs] = 2,
            [MetricType.MergedPRs] = 20,
            [MetricType.BlockedItems] = 0
        };

        dbContext.EngineeringMetrics.AddRange(values.Select(metric =>
            new EngineeringMetric
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                MetricType = metric.Key,
                Value = metric.Value,
                DataStatus = MetricDataStatus.Available,
                PeriodStart = new DateOnly(2026, 9, 1),
                PeriodEnd = new DateOnly(2026, 9, 30),
                CreatedAt = DateTimeOffset.UtcNow
            }));
        await dbContext.SaveChangesAsync();
    }
}
