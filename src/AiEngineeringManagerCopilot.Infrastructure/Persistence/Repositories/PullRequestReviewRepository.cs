using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class PullRequestReviewRepository(
    AppDbContext dbContext)
    : IPullRequestReviewRepository
{
    public async Task<IReadOnlyList<PullRequestReview>>
        GetByPullRequestIdsAsync(
            IReadOnlyCollection<Guid> pullRequestIds,
            CancellationToken cancellationToken)
    {
        if (pullRequestIds.Count == 0)
        {
            return [];
        }

        return await dbContext.PullRequestReviews
            .Where(x =>
                pullRequestIds.Contains(x.PullRequestId))
            .OrderBy(x => x.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<PullRequestReview?> GetByExternalIdAsync(
        Guid pullRequestId,
        long externalId,
        CancellationToken cancellationToken)
    {
        return dbContext.PullRequestReviews
            .SingleOrDefaultAsync(
                x =>
                    x.PullRequestId == pullRequestId &&
                    x.ExternalId == externalId,
                cancellationToken);
    }

    public async Task<PullRequestReview> UpsertAsync(
        PullRequestReview review,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO pull_request_reviews
                 ("Id",
                  "ExternalId",
                  "PullRequestId",
                  "ReviewerExternalId",
                  "SubmittedAt",
                  "State")
             VALUES
                 ({review.Id},
                  {review.ExternalId},
                  {review.PullRequestId},
                  {review.ReviewerExternalId},
                  {review.SubmittedAt},
                  {review.State.ToString()})
             ON CONFLICT
                 ("PullRequestId", "ExternalId")
             DO UPDATE SET
                 "ReviewerExternalId" = EXCLUDED."ReviewerExternalId",
                 "SubmittedAt" = EXCLUDED."SubmittedAt",
                 "State" = EXCLUDED."State";
             """,
            cancellationToken);

        return await dbContext.PullRequestReviews
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.PullRequestId == review.PullRequestId &&
                    x.ExternalId == review.ExternalId,
                cancellationToken);
    }

    public async Task AddAsync(
        PullRequestReview review,
        CancellationToken cancellationToken)
    {
        await dbContext.PullRequestReviews.AddAsync(
            review,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}