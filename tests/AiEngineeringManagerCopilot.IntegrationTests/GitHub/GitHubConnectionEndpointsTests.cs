using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.GitHub;

public sealed class GitHubConnectionEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeGitHubClient _fakeGitHubClient;
    private readonly CustomWebApplicationFactory _factory;

    public GitHubConnectionEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _fakeGitHubClient =
            factory.Services.GetRequiredService<FakeGitHubClient>();
        
        _fakeGitHubClient.Reset();
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
        string organization,
        string accessToken)
    {
        var request = new
        {
            Organization = organization,
            AccessToken = accessToken
        };

        var response = await _client.PostAsJsonAsync(
            $"/teams/{teamId}/github",
            request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateGitHubConnection_ShouldReturnCreated()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                "github-secret-token"));

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var connection = await response.Content
            .ReadFromJsonAsync<GitHubConnectionResponse>();

        connection.Should().NotBeNull();
        connection!.TeamId.Should().Be(team.Id);
        connection.Organization.Should().Be("my-company");
        connection.LastSyncAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateGitHubConnection_ShouldNotReturnAccessToken()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                "github-secret-token"));

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var body = await response.Content
            .ReadAsStringAsync();

        body.Should()
            .NotContain("github-secret-token");

        body.Should()
            .NotContain("accessToken");
    }

    [Fact]
    public async Task CreateGitHubConnection_WithUnknownTeam_ShouldReturnNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            $"/teams/{Guid.NewGuid()}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                "github-secret-token"));

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateGitHubConnection_WithEmptyOrganization_ShouldReturnBadRequest()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "",
                "github-secret-token"));

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGitHubConnection_WithEmptyToken_ShouldReturnBadRequest()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                ""));

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGitHubConnection_WhenAlreadyExists_ShouldReturnConflict()
    {
        var team = await CreateTeamAsync();

        var firstResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                "github-token-1"));

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                "github-token-2"));

        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.Conflict);
    }
    
    [Fact]
    public async Task GetGitHubConnection_ShouldReturnConnection()
    {
        var team = await CreateTeamAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                "github-secret-token"));

        createResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var response = await _client.GetAsync(
            $"/teams/{team.Id}/github");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var connection = await response.Content
            .ReadFromJsonAsync<GitHubConnectionResponse>();

        connection.Should().NotBeNull();
        connection!.TeamId.Should().Be(team.Id);
        connection.Organization.Should().Be("my-company");
        connection.LastSyncAt.Should().BeNull();
    }
    
    [Fact]
    public async Task GetGitHubConnection_WhenNotConfigured_ShouldReturnNotFound()
    {
        var team = await CreateTeamAsync();

        var response = await _client.GetAsync(
            $"/teams/{team.Id}/github");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetGitHubConnection_WithUnknownTeam_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync(
            $"/teams/{Guid.NewGuid()}/github");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetGitHubConnection_ShouldNotExposeAccessToken()
    {
        var team = await CreateTeamAsync();

        await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                "github-secret-token"));

        var response = await _client.GetAsync(
            $"/teams/{team.Id}/github");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();

        body.Should()
            .NotContain("github-secret-token");

        body.Should()
            .NotContain("accessToken");

        body.Should()
            .NotContain("accessTokenEncrypted");
    }
    
    [Fact]
    public async Task DeleteGitHubConnection_ShouldReturnNoContent()
    {
        var team = await CreateTeamAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                "github-secret-token"));

        createResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var response = await _client.DeleteAsync(
            $"/teams/{team.Id}/github");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);
    }
    
    [Fact]
    public async Task DeleteGitHubConnection_WhenNotConfigured_ShouldReturnNotFound()
    {
        var team = await CreateTeamAsync();

        var response = await _client.DeleteAsync(
            $"/teams/{team.Id}/github");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task DeleteGitHubConnection_WithUnknownTeam_ShouldReturnNotFound()
    {
        var response = await _client.DeleteAsync(
            $"/teams/{Guid.NewGuid()}/github");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task DeleteGitHubConnection_ShouldRemoveConnection()
    {
        var team = await CreateTeamAsync();

        await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/github",
            new CreateGitHubConnectionRequest(
                "my-company",
                "github-secret-token"));

        var deleteResponse = await _client.DeleteAsync(
            $"/teams/{team.Id}/github");

        deleteResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(
            $"/teams/{team.Id}/github");

        getResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task TestConnection_ShouldReturn200_WhenConnectionIsValid()
    {
        // Arrange
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        await CreateGitHubConnectionAsync(
            teamId,
            "my-company",
            "secret-token");

        // Act
        var response = await _client.PostAsync(
            $"/teams/{teamId}/github/test",
            null);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<
                TestGitHubConnectionResponse>();

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Organization.Should().Be("my-company");
        result.Message.Should().Be(
            "GitHub connection is valid.");
    }
    
    [Fact]
    public async Task TestConnection_ShouldReturnFailedResult_WhenOrganizationIsNotFound()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        await CreateGitHubConnectionAsync(
            teamId,
            "unknown-company",
            "secret-token");

        _fakeGitHubClient.ShouldReturnOrganization = false;

        var response = await _client.PostAsync(
            $"/teams/{teamId}/github/test",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<
                TestGitHubConnectionResponse>();

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Organization.Should().Be("unknown-company");
    }
    
    [Fact]
    public async Task TestConnection_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/github/test",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task TestConnection_ShouldReturn404_WhenGitHubIsNotConfigured()
    {
        var teamId = await CreateTeamAsync();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/github/test",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task TestConnection_ShouldNotExposeAccessToken()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        const string accessToken = "super-secret-github-token";

        await CreateGitHubConnectionAsync(
            teamId,
            "my-company",
            accessToken);

        var response = await _client.PostAsync(
            $"/teams/{teamId}/github/test",
            null);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContain(accessToken);
    }
    
    [Fact]
    public async Task TestConnection_ShouldDecryptAndPassAccessTokenToGitHubClient()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        const string accessToken = "super-secret-github-token";

        await CreateGitHubConnectionAsync(
            teamId,
            "my-company",
            accessToken);

        await _client.PostAsync(
            $"/teams/{teamId}/github/test",
            null);

        _fakeGitHubClient.ReceivedOrganization
            .Should()
            .Be("my-company");

        _fakeGitHubClient.ReceivedAccessToken
            .Should()
            .Be(accessToken);
    }
    
    [Fact]
    public async Task GetGitHubConnection_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Unauthenticated",
            "true");

        var teamId = Guid.NewGuid();

        var response = await client.GetAsync(
            $"/teams/{teamId}/github");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }
    
}