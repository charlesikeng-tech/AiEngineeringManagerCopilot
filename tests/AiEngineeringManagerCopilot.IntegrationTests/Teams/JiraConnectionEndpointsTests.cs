using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Teams;

public class JiraConnectionEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public JiraConnectionEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateJiraConnection_ShouldPersistConnectionWithoutExposingApiToken()
    {
        // Arrange
        var team = await CreateTeamAsync();

        var request = new CreateJiraConnectionRequest(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "my-secret-jira-token",
            "rec");

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/jira",
            request);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var result = await response.Content
            .ReadFromJsonAsync<JiraConnectionResponse>();

        result.Should().NotBeNull();

        result!.TeamId.Should()
            .Be(team.Id);

        result.BaseUrl.Should()
            .Be("https://my-company.atlassian.net");

        result.Email.Should()
            .Be("charles@example.com");

        result.ProjectKey.Should()
            .Be("REC");

        result.LastSyncAt.Should()
            .BeNull();

        // Verify persisted data
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var connection = await dbContext.JiraConnections
            .SingleAsync(x => x.TeamId == team.Id);

        connection.BaseUrl.Should()
            .Be("https://my-company.atlassian.net");

        connection.Email.Should()
            .Be("charles@example.com");

        connection.ProjectKey.Should()
            .Be("REC");

        connection.ApiTokenEncrypted.Should()
            .NotBe("my-secret-jira-token");

        connection.ApiTokenEncrypted.Should()
            .NotBeNullOrWhiteSpace();

        // The API must never expose the token.
        var responseBody = await response.Content
            .ReadAsStringAsync();

        responseBody.Should()
            .NotContain("my-secret-jira-token");

        responseBody.Should()
            .NotContain("apiTokenEncrypted");
    }

    [Fact]
    public async Task GetJiraConnection_ShouldReturnExistingConnection()
    {
        // Arrange
        var team = await CreateTeamAsync();

        var request = new CreateJiraConnectionRequest(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "my-secret-jira-token",
            "REC");

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/jira",
            request);

        createResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        // Act
        var response = await _client.GetAsync(
            $"/teams/{team.Id}/jira");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<JiraConnectionResponse>();

        result.Should().NotBeNull();

        result!.TeamId.Should()
            .Be(team.Id);

        result.BaseUrl.Should()
            .Be("https://my-company.atlassian.net");

        result.Email.Should()
            .Be("charles@example.com");

        result.ProjectKey.Should()
            .Be("REC");
    }

    [Fact]
    public async Task DeleteJiraConnection_ShouldRemoveConnection()
    {
        // Arrange
        var team = await CreateTeamAsync();

        var request = new CreateJiraConnectionRequest(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "my-secret-jira-token",
            "REC");

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/jira",
            request);

        createResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        // Act
        var deleteResponse = await _client.DeleteAsync(
            $"/teams/{team.Id}/jira");

        // Assert
        deleteResponse.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(
            $"/teams/{team.Id}/jira");

        getResponse.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateJiraConnection_WhenConnectionAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var team = await CreateTeamAsync();

        var request = new CreateJiraConnectionRequest(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "my-secret-jira-token",
            "REC");

        var firstResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/jira",
            request);

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        // Act
        var secondResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/jira",
            request);

        // Assert
        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TestJiraConnection_ShouldDecryptTokenAndValidateConnection()
    {
        // Arrange
        var team = await CreateTeamAsync();

        var createRequest = new CreateJiraConnectionRequest(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "my-secret-jira-token",
            "REC");

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/jira",
            createRequest);

        createResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var fakeJiraClient = _factory.Services
            .GetRequiredService<FakeJiraClient>();

        fakeJiraClient.Reset();

        fakeJiraClient.CurrentUser = new JiraCurrentUser(
            "jira-account-123",
            "Charles Ikeng",
            "charles@example.com");

        // Act
        var response = await _client.PostAsync(
            $"/teams/{team.Id}/jira/test",
            null);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<TestJiraConnectionResponse>();

        result.Should().NotBeNull();

        result!.IsValid.Should()
            .BeTrue();

        result.DisplayName.Should()
            .Be("Charles Ikeng");

        result.Message.Should()
            .Be("Jira connection is valid.");

        // Verify that the decrypted credentials were sent
        // to the Jira client.
        fakeJiraClient.ReceivedBaseUrl.Should()
            .Be("https://my-company.atlassian.net");

        fakeJiraClient.ReceivedEmail.Should()
            .Be("charles@example.com");

        fakeJiraClient.ReceivedApiToken.Should()
            .Be("my-secret-jira-token");
    }
    
    [Fact]
    public async Task SyncJiraConnection_ShouldPersistIssues()
    {
        // Arrange
        var team = await CreateTeamAsync();

        var createRequest = new CreateJiraConnectionRequest(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "my-secret-jira-token",
            "REC");

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/jira",
            createRequest);

        createResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var fakeJiraClient = _factory.Services
            .GetRequiredService<FakeJiraClient>();

        fakeJiraClient.Reset();

        fakeJiraClient.Issues =
        [
            new JiraIssue(
                "10001",
                "REC-123",
                "Improve recommendation ranking",
                "In Progress",
                "jira-user-123",
                DateTimeOffset.Parse(
                    "2026-09-10T10:00:00Z"),
                null,
                false),

            new JiraIssue(
                "10002",
                "REC-124",
                "Fix similar ads API",
                "To Do",
                null,
                DateTimeOffset.Parse(
                    "2026-09-11T10:00:00Z"),
                null,
                false)
        ];

        // Act
        var response = await _client.PostAsync(
            $"/teams/{team.Id}/jira/sync",
            null);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<JiraSyncResult>();

        result.Should().NotBeNull();

        result!.Created.Should().Be(2);
        result.Updated.Should().Be(0);
        result.Total.Should().Be(2);

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var workItems = await dbContext.JiraWorkItems
            .Where(x => x.TeamId == team.Id)
            .OrderBy(x => x.Key)
            .ToListAsync();

        workItems.Should().HaveCount(2);

        workItems[0].Key.Should()
            .Be("REC-123");

        workItems[1].Key.Should()
            .Be("REC-124");

        fakeJiraClient.ReceivedProjectKey.Should()
            .Be("REC");

        fakeJiraClient.ReceivedApiToken.Should()
            .Be("my-secret-jira-token");
    }
    
    [Fact]
    public async Task JiraSync_ShouldFeedLeadTimeMetric()
    {
        // Arrange
        var team = await CreateTeamAsync();

        var createRequest = new CreateJiraConnectionRequest(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "my-secret-jira-token",
            "REC");

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/jira",
            createRequest);

        createResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var fakeJiraClient = _factory.Services
            .GetRequiredService<FakeJiraClient>();

        fakeJiraClient.Reset();

        fakeJiraClient.Issues =
        [
            new JiraIssue(
                "10001",
                "REC-123",
                "Improve recommendation ranking",
                "Done",
                "jira-user-123",
                DateTimeOffset.Parse(
                    "2026-09-10T08:00:00Z"),
                DateTimeOffset.Parse(
                    "2026-09-11T20:00:00Z"),
                false)
        ];

        // Act - Sync Jira
        var syncResponse = await _client.PostAsync(
            $"/teams/{team.Id}/jira/sync",
            null);

        syncResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        // Act - Calculate Lead Time
        var metricResponse = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/lead-time" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        // Assert
        metricResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var metric = await metricResponse.Content
            .ReadFromJsonAsync<EngineeringMetricResponse>();

        metric.Should().NotBeNull();

        metric!.MetricType.Should()
            .Be(MetricType.LeadTime);

        metric.Value.Should()
            .Be(36);
    }

    private async Task<TeamResponse> CreateTeamAsync()
    {
        var request = new CreateTeamRequest(
            $"Jira Team {Guid.NewGuid()}",
            null);

        var response = await _client.PostAsJsonAsync(
            "/teams",
            request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var team = await response.Content
            .ReadFromJsonAsync<TeamResponse>();

        team.Should().NotBeNull();

        return team!;
    }
}