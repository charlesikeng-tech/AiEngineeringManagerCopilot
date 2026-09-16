using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
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
                x.PullRequest.CreatedAt >= start &&
                x.PullRequest.CreatedAt < end)
            .Select(x => x.PullRequest)
            .OrderBy(x => x.CreatedAt)
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
}