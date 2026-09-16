using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Teams;

public sealed class GitHubSyncEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GitHubSyncEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<TeamResponse> CreateTeamAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/teams",
            new CreateTeamRequest(
                $"Team-{Guid.NewGuid():N}",
                null));

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var team = await response.Content
            .ReadFromJsonAsync<TeamResponse>();

        team.Should().NotBeNull();

        return team!;
    }

    private async Task CreateGitHubConnectionAsync(
        Guid teamId,
        string organization = "my-company",
        string accessToken = "secret-token")
    {
        var response = await _client.PostAsJsonAsync(
            $"/teams/{teamId}/github",
            new CreateGitHubConnectionRequest(
                organization,
                accessToken));

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);
    }

    private FakeGitHubClient GetFakeGitHubClient()
    {
        return _factory.Services
            .GetRequiredService<FakeGitHubClient>();
    }

    [Fact]
    public async Task SyncGitHubRepositories_ShouldCreateRepositories()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main"),

            new GitHubRepository(
                1002,
                "frontend",
                "my-company/frontend",
                "https://github.com/my-company/frontend",
                "main")
        ];

        // Act
        var response = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<GitHubSyncResponse>();

        result.Should().NotBeNull();
        result!.Synchronized.Should().Be(2);
        result.Created.Should().Be(2);
        result.Updated.Should().Be(0);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldUpdateExistingRepositories()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main")
        ];

        // First sync: creates the repository.
        var firstSyncResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        firstSyncResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var firstResult =
            await firstSyncResponse.Content
                .ReadFromJsonAsync<GitHubSyncResponse>();

        firstResult.Should().NotBeNull();
        firstResult!.Created.Should().Be(1);
        firstResult.Updated.Should().Be(0);

        // Change data returned by GitHub.
        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend-api",
                "my-company/backend-api",
                "https://github.com/my-company/backend-api",
                "develop")
        ];

        // Second sync: should update the existing repository.
        var secondSyncResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        secondSyncResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var secondResult =
            await secondSyncResponse.Content
                .ReadFromJsonAsync<GitHubSyncResponse>();

        secondResult.Should().NotBeNull();
        secondResult!.Synchronized.Should().Be(1);
        secondResult.Created.Should().Be(0);
        secondResult.Updated.Should().Be(1);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldBeIdempotent()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main"),

            new GitHubRepository(
                1002,
                "frontend",
                "my-company/frontend",
                "https://github.com/my-company/frontend",
                "main")
        ];

        // Act - first synchronization
        var firstResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var firstResult =
            await firstResponse.Content
                .ReadFromJsonAsync<GitHubSyncResponse>();

        // Act - second synchronization with exactly the same data
        var secondResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var secondResult =
            await secondResponse.Content
                .ReadFromJsonAsync<GitHubSyncResponse>();

        // Assert
        firstResult.Should().NotBeNull();
        firstResult!.Synchronized.Should().Be(2);
        firstResult.Created.Should().Be(2);
        firstResult.Updated.Should().Be(0);

        secondResult.Should().NotBeNull();
        secondResult!.Synchronized.Should().Be(2);
        secondResult.Created.Should().Be(0);
        secondResult.Updated.Should().Be(2);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/github/sync",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldReturn404_WhenGitHubIsNotConfigured()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldDecryptAndPassAccessTokenToGitHubClient()
    {
        // Arrange
        var team = await CreateTeamAsync();

        const string accessToken = "super-secret-github-token";

        await CreateGitHubConnectionAsync(
            team.Id,
            "my-company",
            accessToken);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main")
        ];

        // Act
        var response = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        fakeGitHubClient.ReceivedOrganization
            .Should()
            .Be("my-company");

        fakeGitHubClient.ReceivedAccessToken
            .Should()
            .Be(accessToken);

        var body = await response.Content.ReadAsStringAsync();

        body.Should()
            .NotContain(accessToken);
    }
}