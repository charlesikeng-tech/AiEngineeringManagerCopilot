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
}