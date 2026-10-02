using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Jira;

public sealed class JiraWorkItemRepositoryConcurrencyTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public JiraWorkItemRepositoryConcurrencyTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpsertAsync_WhenCalledConcurrentlyForSameBusinessKey_ShouldPersistSingleWorkItem()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        const string externalId = "10001";

        var firstCreatedAt =
            DateTimeOffset.Parse(
                "2026-09-10T10:00:00Z");

        var secondCreatedAt =
            DateTimeOffset.Parse(
                "2026-09-10T11:00:00Z");

        var firstDoneAt =
            DateTimeOffset.Parse(
                "2026-09-20T15:30:00Z");

        var first = new JiraWorkItem
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ExternalId = externalId,
            Key = "KME-123",
            Summary = "First summary",
            Status = "Done",
            AssigneeExternalId = "jira-user-1",
            CreatedAt = firstCreatedAt,
            DoneAt = firstDoneAt,
            IsBlocked = true
        };

        var second = new JiraWorkItem
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ExternalId = externalId,
            Key = "KME-123",
            Summary = "Second summary",
            Status = "In Progress",
            AssigneeExternalId = "jira-user-2",
            CreatedAt = secondCreatedAt,
            DoneAt = null,
            IsBlocked = false
        };

        // Act
        var act = async () =>
        {
            await Task.WhenAll(
                UpsertAsync(first),
                UpsertAsync(second));
        };

        // Assert
        await act.Should().NotThrowAsync();

        using var assertScope =
            _factory.Services.CreateScope();

        var dbContext = assertScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var persisted = await dbContext.JiraWorkItems
            .AsNoTracking()
            .Where(x =>
                x.TeamId == teamId &&
                x.ExternalId == externalId)
            .ToListAsync();

        persisted.Should().ContainSingle();

        var workItem = persisted.Single();

        var matchesFirst =
            workItem.Key == first.Key &&
            workItem.Summary == first.Summary &&
            workItem.Status == first.Status &&
            workItem.AssigneeExternalId ==
                first.AssigneeExternalId &&
            workItem.CreatedAt == first.CreatedAt &&
            workItem.DoneAt == first.DoneAt &&
            workItem.IsBlocked == first.IsBlocked;

        var matchesSecond =
            workItem.Key == second.Key &&
            workItem.Summary == second.Summary &&
            workItem.Status == second.Status &&
            workItem.AssigneeExternalId ==
                second.AssigneeExternalId &&
            workItem.CreatedAt == second.CreatedAt &&
            workItem.DoneAt == second.DoneAt &&
            workItem.IsBlocked == second.IsBlocked;

        (matchesFirst || matchesSecond)
            .Should()
            .BeTrue(
                "the persisted Jira work item must correspond " +
                "to one complete concurrent write and must not " +
                "contain a hybrid state");
    }

    private async Task UpsertAsync(
        JiraWorkItem workItem)
    {
        using var scope =
            _factory.Services.CreateScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<IJiraWorkItemRepository>();

        await repository.UpsertAsync(
            workItem,
            CancellationToken.None);
    }

    private async Task<Guid> CreateTeamAsync()
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
            Name =
                $"Jira Concurrency Team {Guid.NewGuid()}",
            Description = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Teams.Add(team);

        await dbContext.SaveChangesAsync();

        return team.Id;
    }
}