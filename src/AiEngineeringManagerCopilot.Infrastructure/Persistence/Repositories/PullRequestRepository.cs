using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class PullRequestRepository(
    AppDbContext dbContext)
    : IPullRequestRepository
{
    public async Task<IReadOnlyList<PullRequest>>
        GetMergedByTeamAndPeriodAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(
            periodStart.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var end = new DateTimeOffset(
            periodEnd
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        return await dbContext.PullRequests
            .Join(
                dbContext.Repositories,
                pullRequest => pullRequest.RepositoryId,
                repository => repository.Id,
                (pullRequest, repository) => new
                {
                    PullRequest = pullRequest,
                    repository.TeamId
                })
            .Where(x =>
                x.TeamId == teamId &&
                x.PullRequest.MergedAt.HasValue &&
                x.PullRequest.MergedAt.Value >= start &&
                x.PullRequest.MergedAt.Value < end)
            .Select(x => x.PullRequest)
            .OrderBy(x => x.MergedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<PullRequest>>
        GetByTeamAndPeriodAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(
            periodStart.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var end = new DateTimeOffset(
            periodEnd
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        return await dbContext.PullRequests
            .Join(
                dbContext.Repositories,
                pullRequest => pullRequest.RepositoryId,
                repository => repository.Id,
                (pullRequest, repository) => new
                {
                    PullRequest = pullRequest,
                    repository.TeamId
                })
            .Where(x =>
                x.TeamId == teamId &&
                x.PullRequest.CreatedAt >= start &&
                x.PullRequest.CreatedAt < end)
            .Select(x => x.PullRequest)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<PullRequest?> GetByExternalIdAsync(
        Guid repositoryId,
        long externalId,
        CancellationToken cancellationToken)
    {
        return dbContext.PullRequests
            .SingleOrDefaultAsync(
                x =>
                    x.RepositoryId == repositoryId &&
                    x.ExternalId == externalId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PullRequest>>
        GetOpenByTeamAtDateAsync(
            Guid teamId,
            DateOnly date,
            CancellationToken cancellationToken)
    {
        var end = new DateTimeOffset(
            date
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        return await dbContext.PullRequests
            .Join(
                dbContext.Repositories,
                pullRequest => pullRequest.RepositoryId,
                repository => repository.Id,
                (pullRequest, repository) => new
                {
                    PullRequest = pullRequest,
                    repository.TeamId
                })
            .Where(x =>
                x.TeamId == teamId &&
                x.PullRequest.CreatedAt < end &&
                (
                    !x.PullRequest.ClosedAt.HasValue ||
                    x.PullRequest.ClosedAt.Value >= end
                ))
            .Select(x => x.PullRequest)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PullRequest> UpsertAsync(
        PullRequest pullRequest,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO pull_requests
                 ("Id",
                  "RepositoryId",
                  "ExternalId",
                  "AuthorExternalId",
                  "Title",
                  "State",
                  "CreatedAt",
                  "MergedAt",
                  "ClosedAt",
                  "IsBlocked")
             VALUES
                 ({pullRequest.Id},
                  {pullRequest.RepositoryId},
                  {pullRequest.ExternalId},
                  {pullRequest.AuthorExternalId},
                  {pullRequest.Title},
                  {pullRequest.State.ToString()},
                  {pullRequest.CreatedAt},
                  {pullRequest.MergedAt},
                  {pullRequest.ClosedAt},
                  {pullRequest.IsBlocked})
             ON CONFLICT
                 ("RepositoryId", "ExternalId")
             DO UPDATE SET
                 "AuthorExternalId" = EXCLUDED."AuthorExternalId",
                 "Title" = EXCLUDED."Title",
                 "State" = EXCLUDED."State",
                 "CreatedAt" = EXCLUDED."CreatedAt",
                 "MergedAt" = EXCLUDED."MergedAt",
                 "ClosedAt" = EXCLUDED."ClosedAt";
             """,
            cancellationToken);

        return await dbContext.PullRequests
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.RepositoryId == pullRequest.RepositoryId &&
                    x.ExternalId == pullRequest.ExternalId,
                cancellationToken);
    }


    public async Task AddAsync(
        PullRequest pullRequest,
        CancellationToken cancellationToken)
    {
        await dbContext.PullRequests.AddAsync(
            pullRequest,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}