using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class JiraWorkItemRepository(
    AppDbContext dbContext)
    : IJiraWorkItemRepository
{
    public Task<JiraWorkItem?> GetByExternalIdAsync(
        Guid teamId,
        string externalId,
        CancellationToken cancellationToken)
    {
        return dbContext.JiraWorkItems
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.TeamId == teamId &&
                    x.ExternalId == externalId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<JiraWorkItem>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        return await dbContext.JiraWorkItems
            .Where(x => x.TeamId == teamId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(
        JiraWorkItem workItem,
        CancellationToken cancellationToken)
    {
        return dbContext.JiraWorkItems
            .AddAsync(
                workItem,
                cancellationToken)
            .AsTask();
    }

    public async Task<JiraWorkItem> UpsertAsync(
        JiraWorkItem workItem,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO jira_work_items
                 ("Id",
                  "TeamId",
                  "ExternalId",
                  "Key",
                  "Summary",
                  "Status",
                  "AssigneeExternalId",
                  "CreatedAt",
                  "DoneAt",
                  "IsBlocked")
             VALUES
                 ({workItem.Id},
                  {workItem.TeamId},
                  {workItem.ExternalId},
                  {workItem.Key},
                  {workItem.Summary},
                  {workItem.Status},
                  {workItem.AssigneeExternalId},
                  {workItem.CreatedAt},
                  {workItem.DoneAt},
                  {workItem.IsBlocked})
             ON CONFLICT ("TeamId", "ExternalId")
             DO UPDATE SET
                 "Key" = EXCLUDED."Key",
                 "Summary" = EXCLUDED."Summary",
                 "Status" = EXCLUDED."Status",
                 "AssigneeExternalId" = EXCLUDED."AssigneeExternalId",
                 "CreatedAt" = EXCLUDED."CreatedAt",
                 "DoneAt" = EXCLUDED."DoneAt",
                 "IsBlocked" = EXCLUDED."IsBlocked";
             """,
            cancellationToken);

        return await dbContext.JiraWorkItems
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.TeamId == workItem.TeamId &&
                    x.ExternalId == workItem.ExternalId,
                cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyList<JiraWorkItem>> GetByTeamAndPeriodAsync(
        Guid teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        return await dbContext.JiraWorkItems
            .Where(x =>
                x.TeamId == teamId &&
                x.CreatedAt >= from &&
                x.CreatedAt <= to)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JiraWorkItem>>
        GetCompletedByTeamAndPeriodAsync(
            Guid teamId,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken)
    {
        return await dbContext.JiraWorkItems
            .Where(x =>
                x.TeamId == teamId &&
                x.DoneAt.HasValue &&
                x.DoneAt.Value >= from &&
                x.DoneAt.Value <= to)
            .OrderBy(x => x.DoneAt)
            .ToListAsync(cancellationToken);
    }
}