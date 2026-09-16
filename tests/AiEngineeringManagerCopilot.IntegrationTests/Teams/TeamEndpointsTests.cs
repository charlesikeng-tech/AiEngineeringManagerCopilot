using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.IntegrationTests.Teams;

public sealed class TeamEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TeamEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTeam_ShouldReturnCreated()
    {
        var request = new CreateTeamRequest(
            "Platform Engineering",
            "Core platform team");

        var response = await _client.PostAsJsonAsync(
            "/teams",
            request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var team = await response.Content
            .ReadFromJsonAsync<TeamResponse>();

        team.Should().NotBeNull();
        team!.Name.Should()
            .Be("Platform Engineering");

        team.Description.Should()
            .Be("Core platform team");
    }
    
    [Fact]
    public async Task CreateTeam_WithEmptyName_ShouldReturnBadRequest()
    {
        var request = new CreateTeamRequest(
            "",
            "Invalid team");

        var response = await _client.PostAsJsonAsync(
            "/teams",
            request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task CreateTeam_WithTooLongName_ShouldReturnBadRequest()
    {
        var request = new CreateTeamRequest(
            new string('A', 201),
            null);

        var response = await _client.PostAsJsonAsync(
            "/teams",
            request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task GetTeam_ShouldReturnCreatedTeam()
    {
        var createRequest = new CreateTeamRequest(
            "Backend Team",
            "Backend services");

        var createResponse = await _client.PostAsJsonAsync(
            "/teams",
            createRequest);

        createResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var createdTeam = await createResponse.Content
            .ReadFromJsonAsync<TeamResponse>();

        createdTeam.Should().NotBeNull();

        var response = await _client.GetAsync(
            $"/teams/{createdTeam!.Id}");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var team = await response.Content
            .ReadFromJsonAsync<TeamResponse>();

        team.Should().NotBeNull();
        team!.Id.Should().Be(createdTeam.Id);
        team.Name.Should().Be("Backend Team");
    }
    
    [Fact]
    public async Task GetTeam_WithUnknownId_ShouldReturnNotFound()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/teams/{teamId}");

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task UpdateTeam_ShouldReturnUpdatedTeam()
    {
        var createRequest = new CreateTeamRequest(
            "Old Name",
            "Old description");

        var createResponse = await _client.PostAsJsonAsync(
            "/teams",
            createRequest);

        var createdTeam = await createResponse.Content
            .ReadFromJsonAsync<TeamResponse>();

        createdTeam.Should().NotBeNull();

        var updateRequest = new UpdateTeamRequest(
            "New Name",
            "New description");

        var response = await _client.PutAsJsonAsync(
            $"/teams/{createdTeam!.Id}",
            updateRequest);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var updatedTeam = await response.Content
            .ReadFromJsonAsync<TeamResponse>();

        updatedTeam.Should().NotBeNull();
        updatedTeam!.Name.Should().Be("New Name");
        updatedTeam.Description.Should().Be("New description");
    }
    
    [Fact]
    public async Task DeleteTeam_ShouldReturnNoContent()
    {
        var createRequest = new CreateTeamRequest(
            "Team To Delete",
            null);

        var createResponse = await _client.PostAsJsonAsync(
            "/teams",
            createRequest);

        var createdTeam = await createResponse.Content
            .ReadFromJsonAsync<TeamResponse>();

        createdTeam.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync(
            $"/teams/{createdTeam!.Id}");

        deleteResponse.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(
            $"/teams/{createdTeam.Id}");

        getResponse.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetTeams_ShouldReturnTeamsForCurrentUser()
    {
        await _client.PostAsJsonAsync(
            "/teams",
            new CreateTeamRequest("Team A", null));

        await _client.PostAsJsonAsync(
            "/teams",
            new CreateTeamRequest("Team B", null));

        var response = await _client.GetAsync("/teams");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var teams = await response.Content
            .ReadFromJsonAsync<List<TeamResponse>>();

        teams.Should().NotBeNull();
        teams.Should().Contain(x => x.Name == "Team A");
        teams.Should().Contain(x => x.Name == "Team B");
    }
}