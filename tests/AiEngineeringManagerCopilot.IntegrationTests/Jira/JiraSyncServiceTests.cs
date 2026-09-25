using System.Security.Claims;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Jira;

public class JiraSyncServiceTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public JiraSyncServiceTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SyncAsync_WhenIssuesDoNotExist_ShouldCreateWorkItems()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateJiraConnectionAsync(team.Id);

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

        using var scope =
            _factory.Services.CreateScope();

        var syncService = scope.ServiceProvider
            .GetRequiredService<IJiraSyncService>();

        // Act
        var result = await syncService.SyncAsync(
            team.Id,
            CancellationToken.None);

        // Assert
        result.Created.Should().Be(2);
        result.Updated.Should().Be(0);
        result.Total.Should().Be(2);

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var workItems = await dbContext.JiraWorkItems
            .Where(x => x.TeamId == team.Id)
            .OrderBy(x => x.Key)
            .ToListAsync();

        workItems.Should().HaveCount(2);

        workItems[0].ExternalId.Should()
            .Be("10001");

        workItems[0].Key.Should()
            .Be("REC-123");

        workItems[0].Summary.Should()
            .Be("Improve recommendation ranking");

        workItems[0].Status.Should()
            .Be("In Progress");

        workItems[0].AssigneeExternalId.Should()
            .Be("jira-user-123");

        workItems[0].IsBlocked.Should()
            .BeFalse();

        workItems[0].DoneAt.Should()
            .BeNull();

        fakeJiraClient.ReceivedProjectKey.Should()
            .Be("REC");

        fakeJiraClient.ReceivedApiToken.Should()
            .Be("my-secret-jira-token");

        var connection = await dbContext.JiraConnections
            .SingleAsync(x => x.TeamId == team.Id);

        connection.LastSyncAt.Should()
            .NotBeNull();
    }

    [Fact]
    public async Task SyncAsync_WhenIssueAlreadyExists_ShouldUpdateWithoutCreatingDuplicate()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateJiraConnectionAsync(team.Id);

        using var arrangeScope =
            _factory.Services.CreateScope();

        var arrangeDbContext = arrangeScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        arrangeDbContext.JiraWorkItems.Add(
            new JiraWorkItem
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                ExternalId = "10001",
                Key = "REC-123",
                Summary = "Old summary",
                Status = "To Do",
                AssigneeExternalId = null,
                CreatedAt = DateTimeOffset.Parse(
                    "2026-09-10T10:00:00Z"),
                DoneAt = null,
                IsBlocked = false
            });

        await arrangeDbContext.SaveChangesAsync();

        var fakeJiraClient = _factory.Services
            .GetRequiredService<FakeJiraClient>();

        fakeJiraClient.Reset();

        fakeJiraClient.Issues =
        [
            new JiraIssue(
                "10001",
                "REC-123",
                "Updated summary",
                "In Progress",
                "jira-user-456",
                DateTimeOffset.Parse(
                    "2026-09-10T10:00:00Z"),
                null,
                false)
        ];

        using var actScope =
            _factory.Services.CreateScope();

        var syncService = actScope.ServiceProvider
            .GetRequiredService<IJiraSyncService>();

        // Act
        var result = await syncService.SyncAsync(
            team.Id,
            CancellationToken.None);

        // Assert
        result.Created.Should().Be(0);
        result.Updated.Should().Be(1);
        result.Total.Should().Be(1);

        var dbContext = actScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var workItems = await dbContext.JiraWorkItems
            .Where(x => x.TeamId == team.Id)
            .ToListAsync();

        workItems.Should().HaveCount(1);

        var workItem = workItems.Single();

        workItem.ExternalId.Should()
            .Be("10001");

        workItem.Key.Should()
            .Be("REC-123");

        workItem.Summary.Should()
            .Be("Updated summary");

        workItem.Status.Should()
            .Be("In Progress");

        workItem.AssigneeExternalId.Should()
            .Be("jira-user-456");
    }
    
    [Fact]
    public async Task SyncAsync_WhenJiraClientFails_ShouldNotPersistChanges()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateJiraConnectionAsync(team.Id);

        var fakeJiraClient = _factory.Services
            .GetRequiredService<FakeJiraClient>();

        fakeJiraClient.Reset();

        fakeJiraClient.ExceptionToThrow =
            new HttpRequestException(
                "Jira unavailable");

        using var scope =
            _factory.Services.CreateScope();

        var syncService = scope.ServiceProvider
            .GetRequiredService<IJiraSyncService>();

        // Act
        var act = async () =>
            await syncService.SyncAsync(
                team.Id,
                CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<HttpRequestException>();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var workItems = await dbContext.JiraWorkItems
            .Where(x => x.TeamId == team.Id)
            .ToListAsync();

        workItems.Should().BeEmpty();

        var connection = await dbContext.JiraConnections
            .SingleAsync(x => x.TeamId == team.Id);

        connection.LastSyncAt.Should().BeNull();
    }
    
    [Fact]
    public async Task SyncAsync_WhenRunTwice_ShouldNotCreateDuplicates()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateJiraConnectionAsync(team.Id);

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
                false)
        ];

        using var scope =
            _factory.Services.CreateScope();

        var syncService = scope.ServiceProvider
            .GetRequiredService<IJiraSyncService>();

        // Act
        var firstResult = await syncService.SyncAsync(
            team.Id,
            CancellationToken.None);

        var secondResult = await syncService.SyncAsync(
            team.Id,
            CancellationToken.None);

        // Assert
        firstResult.Created.Should().Be(1);
        firstResult.Updated.Should().Be(0);

        secondResult.Created.Should().Be(0);
        secondResult.Updated.Should().Be(1);

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var workItems = await dbContext.JiraWorkItems
            .Where(x => x.TeamId == team.Id)
            .ToListAsync();

        workItems.Should().ContainSingle();

        workItems.Single().ExternalId.Should()
            .Be("10001");
    }
    
    [Fact]
    public async Task SyncAsync_WhenIssueStateChanges_ShouldUpdateStateFields()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateJiraConnectionAsync(team.Id);

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
                false)
        ];

        using var scope =
            _factory.Services.CreateScope();

        var syncService = scope.ServiceProvider
            .GetRequiredService<IJiraSyncService>();

        await syncService.SyncAsync(
            team.Id,
            CancellationToken.None);

        var doneAt =
            DateTimeOffset.Parse(
                "2026-09-20T15:30:00Z");

        fakeJiraClient.Issues =
        [
            new JiraIssue(
                "10001",
                "REC-123",
                "Improve recommendation ranking",
                "Done",
                "jira-user-123",
                DateTimeOffset.Parse(
                    "2026-09-10T10:00:00Z"),
                doneAt,
                true)
        ];

        // Act
        var result = await syncService.SyncAsync(
            team.Id,
            CancellationToken.None);

        // Assert
        result.Created.Should().Be(0);
        result.Updated.Should().Be(1);
        result.Total.Should().Be(1);

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var workItem = await dbContext.JiraWorkItems
            .SingleAsync(x =>
                x.TeamId == team.Id &&
                x.ExternalId == "10001");

        workItem.Status.Should().Be("Done");

        workItem.DoneAt.Should().Be(doneAt);

        workItem.IsBlocked.Should().BeTrue();
    }
    
    [Fact]
    public async Task SyncAsync_WhenSuccessful_ShouldPersistWorkItemsAndLastSyncAt()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateJiraConnectionAsync(team.Id);

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
                false)
        ];

        using var scope =
            _factory.Services.CreateScope();

        var syncService = scope.ServiceProvider
            .GetRequiredService<IJiraSyncService>();

        // Act
        var result = await syncService.SyncAsync(
            team.Id,
            CancellationToken.None);

        // Assert
        result.Created.Should().Be(1);

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var workItem = await dbContext.JiraWorkItems
            .SingleAsync(x =>
                x.TeamId == team.Id &&
                x.ExternalId == "10001");

        workItem.Key.Should().Be("REC-123");

        var connection = await dbContext.JiraConnections
            .SingleAsync(x => x.TeamId == team.Id);

        connection.LastSyncAt.Should().NotBeNull();
    }

    private async Task<TeamResponse> CreateTeamAsync()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var team = new Team
        {
            Id = Guid.NewGuid(),
            OwnerUserId = Guid.Parse(
                "11111111-1111-1111-1111-111111111111"),
            Name = $"Jira Sync Team {Guid.NewGuid()}",
            Description = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Teams.Add(team);

        await dbContext.SaveChangesAsync();

        return TeamResponse.FromEntity(team);
    }

    private async Task CreateJiraConnectionAsync(
        Guid teamId)
    {
        using var scope =
            _factory.Services.CreateScope();

        AuthenticateTestUser(scope.ServiceProvider);
        
        var jiraConnectionService =
            scope.ServiceProvider
                .GetRequiredService<IJiraConnectionService>();

        var result =
            await jiraConnectionService.CreateAsync(
                teamId,
                new CreateJiraConnectionRequest(
                    "https://my-company.atlassian.net",
                    "charles@example.com",
                    "my-secret-jira-token",
                    "REC"),
                CancellationToken.None);

        result.Should().NotBeNull();
    }
    
    private static void AuthenticateTestUser(
        IServiceProvider serviceProvider)
    {
        var httpContextAccessor = serviceProvider
            .GetRequiredService<IHttpContextAccessor>();

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                "11111111-1111-1111-1111-111111111111")
        };

        var identity = new ClaimsIdentity(
            claims,
            "Test");

        httpContextAccessor.HttpContext =
            new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };
    }
}