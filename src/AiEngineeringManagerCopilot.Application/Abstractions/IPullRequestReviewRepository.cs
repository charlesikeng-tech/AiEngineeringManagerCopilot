using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IPullRequestReviewRepository
{
    Task<IReadOnlyList<PullRequestReview>>
        GetByPullRequestIdsAsync(
            IReadOnlyCollection<Guid> pullRequestIds,
            CancellationToken cancellationToken);
}