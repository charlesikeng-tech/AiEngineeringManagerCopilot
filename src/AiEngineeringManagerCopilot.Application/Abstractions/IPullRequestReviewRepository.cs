using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IPullRequestReviewRepository
{
    Task<IReadOnlyList<PullRequestReview>>
        GetByPullRequestIdsAsync(
            IReadOnlyCollection<Guid> pullRequestIds,
            CancellationToken cancellationToken);

    Task<PullRequestReview?> GetByExternalIdAsync(
        Guid pullRequestId,
        long externalId,
        CancellationToken cancellationToken);

    Task AddAsync(
        PullRequestReview review,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}