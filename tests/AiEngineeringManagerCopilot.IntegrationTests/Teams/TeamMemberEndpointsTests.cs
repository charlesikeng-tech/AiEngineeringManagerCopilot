using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.TeamMembers;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.IntegrationTests.Teams;

public sealed class TeamMemberEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TeamMemberEndpointsTests(
        CustomWebApplicationFactory factory)
    {
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

    [Fact]
    public async Task CreateMember_ShouldReturnCreated()
    {
        var team = await CreateTeamAsync();

        var request = new CreateTeamMemberRequest(
            "John Doe",
            "john.doe@example.com",
            TeamMemberRole.Developer,
            "github-123");

        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var member = await response.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        member.Should().NotBeNull();
        member!.TeamId.Should().Be(team.Id);
        member.Name.Should().Be("John Doe");
        member.Email.Should().Be("john.doe@example.com");
        member.Role.Should().Be(TeamMemberRole.Developer);
        member.ProviderUserId.Should().Be("github-123");
    }

    [Fact]
    public async Task GetMembers_ShouldReturnTeamMembers()
    {
        var team = await CreateTeamAsync();

        await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "Alice",
                "alice@example.com",
                TeamMemberRole.TechLead,
                null));

        var response = await _client.GetAsync(
            $"/teams/{team.Id}/members");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var members = await response.Content
            .ReadFromJsonAsync<List<TeamMemberResponse>>();

        members.Should().NotBeNull();
        members.Should().ContainSingle();
        members![0].Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetMember_ShouldReturnMember()
    {
        var team = await CreateTeamAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "Bob",
                "bob@example.com",
                TeamMemberRole.QA,
                null));

        var createdMember = await createResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        createdMember.Should().NotBeNull();

        var response = await _client.GetAsync(
            $"/teams/{team.Id}/members/{createdMember!.Id}");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var member = await response.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        member.Should().NotBeNull();
        member!.Id.Should().Be(createdMember.Id);
    }

    [Fact]
    public async Task UpdateMember_ShouldReturnUpdatedMember()
    {
        var team = await CreateTeamAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "Old Name",
                "old@example.com",
                TeamMemberRole.Developer,
                null));

        var createdMember = await createResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        createdMember.Should().NotBeNull();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/teams/{team.Id}/members/{createdMember!.Id}",
            new UpdateTeamMemberRequest(
                "New Name",
                "new@example.com",
                TeamMemberRole.TechLead,
                "github-456"));

        updateResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var member = await updateResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        member.Should().NotBeNull();
        member!.Name.Should().Be("New Name");
        member.Email.Should().Be("new@example.com");
        member.Role.Should().Be(TeamMemberRole.TechLead);
        member.ProviderUserId.Should().Be("github-456");
    }

    [Fact]
    public async Task DeleteMember_ShouldReturnNoContent()
    {
        var team = await CreateTeamAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "To Delete",
                "delete@example.com",
                TeamMemberRole.Developer,
                null));

        var member = await createResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        member.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync(
            $"/teams/{team.Id}/members/{member!.Id}");

        deleteResponse.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(
            $"/teams/{team.Id}/members/{member.Id}");

        getResponse.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CreateMember_WithEmptyName_ShouldReturnBadRequest()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "",
                "john@example.com",
                TeamMemberRole.Developer,
                null));

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task CreateMember_WithInvalidEmail_ShouldReturnBadRequest()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "invalid-email",
                TeamMemberRole.Developer,
                null));

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task CreateMember_WithUnknownTeam_ShouldReturnNotFound()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync(
            $"/teams/{teamId}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "john@example.com",
                TeamMemberRole.Developer,
                null));

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetMember_FromDifferentTeam_ShouldReturnNotFound()
    {
        var teamA = await CreateTeamAsync();
        var teamB = await CreateTeamAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{teamA.Id}/members",
            new CreateTeamMemberRequest(
                "Alice",
                "alice@example.com",
                TeamMemberRole.Developer,
                null));

        var member = await createResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        member.Should().NotBeNull();

        var response = await _client.GetAsync(
            $"/teams/{teamB.Id}/members/{member!.Id}");

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CreateMember_WithDuplicateEmail_ShouldReturnConflict()
    {
        var team = await CreateTeamAsync();

        var firstResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "john@example.com",
                TeamMemberRole.Developer,
                "github-100"));

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "Jane Doe",
                "john@example.com",
                TeamMemberRole.Developer,
                "github-101"));

        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.Conflict);
    }
    
    [Fact]
    public async Task CreateMember_WithDuplicateProviderUserId_ShouldReturnConflict()
    {
        var team = await CreateTeamAsync();

        var firstResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "john@example.com",
                TeamMemberRole.Developer,
                "github-100"));

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "Jane Doe",
                "jane@example.com",
                TeamMemberRole.Developer,
                "github-100"));

        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.Conflict);
    }
    
    [Fact]
    public async Task CreateMember_WithSameEmailInDifferentTeams_ShouldSucceed()
    {
        var firstTeam = await CreateTeamAsync();
        var secondTeam = await CreateTeamAsync();

        var firstResponse = await _client.PostAsJsonAsync(
            $"/teams/{firstTeam.Id}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "john@example.com",
                TeamMemberRole.Developer,
                null));

        var secondResponse = await _client.PostAsJsonAsync(
            $"/teams/{secondTeam.Id}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "john@example.com",
                TeamMemberRole.Developer,
                null));

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task CreateMember_WithSameProviderUserIdInDifferentTeams_ShouldSucceed()
    {
        var firstTeam = await CreateTeamAsync();
        var secondTeam = await CreateTeamAsync();

        var firstResponse = await _client.PostAsJsonAsync(
            $"/teams/{firstTeam.Id}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "john1@example.com",
                TeamMemberRole.Developer,
                "github-100"));

        var secondResponse = await _client.PostAsJsonAsync(
            $"/teams/{secondTeam.Id}/members",
            new CreateTeamMemberRequest(
                "Jane Doe",
                "jane@example.com",
                TeamMemberRole.Developer,
                "github-100"));

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task CreateMember_WithNullProviderUserId_ShouldAllowMultipleMembers()
    {
        var team = await CreateTeamAsync();

        var firstResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "john@example.com",
                TeamMemberRole.Developer,
                null));

        var secondResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "Jane Doe",
                "jane@example.com",
                TeamMemberRole.Developer,
                null));

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task UpdateMember_WithDuplicateEmail_ShouldReturnConflict()
    {
        var team = await CreateTeamAsync();

        var firstResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "john@example.com",
                TeamMemberRole.Developer,
                null));

        var firstMember = await firstResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        var secondResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "Jane Doe",
                "jane@example.com",
                TeamMemberRole.Developer,
                null));

        var secondMember = await secondResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        firstMember.Should().NotBeNull();
        secondMember.Should().NotBeNull();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/teams/{team.Id}/members/{secondMember!.Id}",
            new UpdateTeamMemberRequest(
                "Jane Updated",
                "john@example.com",
                TeamMemberRole.Developer,
                null));

        updateResponse.StatusCode.Should()
            .Be(HttpStatusCode.Conflict);
    }
    
    [Fact]
    public async Task UpdateMember_WithDuplicateProviderUserId_ShouldReturnConflict()
    {
        var team = await CreateTeamAsync();

        var firstResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "John Doe",
                "john@example.com",
                TeamMemberRole.Developer,
                "github-100"));

        var firstMember = await firstResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        var secondResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "Jane Doe",
                "jane@example.com",
                TeamMemberRole.Developer,
                "github-200"));

        var secondMember = await secondResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        firstMember.Should().NotBeNull();
        secondMember.Should().NotBeNull();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/teams/{team.Id}/members/{secondMember!.Id}",
            new UpdateTeamMemberRequest(
                "Jane Updated",
                "jane.updated@example.com",
                TeamMemberRole.Developer,
                "github-100"));

        updateResponse.StatusCode.Should()
            .Be(HttpStatusCode.Conflict);
    }
    
    [Fact]
    public async Task UpdateMember_ShouldPersistChanges()
    {
        var team = await CreateTeamAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new CreateTeamMemberRequest(
                "Old Name",
                "old@example.com",
                TeamMemberRole.Developer,
                null));

        var createdMember = await createResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        createdMember.Should().NotBeNull();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/teams/{team.Id}/members/{createdMember!.Id}",
            new UpdateTeamMemberRequest(
                "New Name",
                "new@example.com",
                TeamMemberRole.TechLead,
                "github-456"));

        updateResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync(
            $"/teams/{team.Id}/members/{createdMember.Id}");

        getResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var member = await getResponse.Content
            .ReadFromJsonAsync<TeamMemberResponse>();

        member.Should().NotBeNull();
        member!.Name.Should().Be("New Name");
        member.Email.Should().Be("new@example.com");
        member.Role.Should().Be(TeamMemberRole.TechLead);
        member.ProviderUserId.Should().Be("github-456");
    }
    
    [Fact]
    public async Task CreateMember_WithInvalidRole_ShouldReturnBadRequest()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsJsonAsync(
            $"/teams/{team.Id}/members",
            new
            {
                name = "John Doe",
                email = "john@example.com",
                role = "InvalidRole",
                providerUserId = (string?)null
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.BadRequest);
    }
    
}