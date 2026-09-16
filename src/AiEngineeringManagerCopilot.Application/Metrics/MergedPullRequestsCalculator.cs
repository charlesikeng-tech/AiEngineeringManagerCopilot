using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class MergedPullRequestsCalculator
    : IMergedPullRequestsCalculator
{
    public MergedPullRequestsResult Calculate(
        IReadOnlyCollection<PullRequest> pullRequests,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        if (periodStart > periodEnd)
        {
            throw new ArgumentException(
                "periodStart must be before or equal to periodEnd.");
        }

        var count = pullRequests.Count(x =>
            x.State == PullRequestState.Merged &&
            DateOnly.FromDateTime(
                x.CreatedAt.UtcDateTime) >= periodStart &&
            DateOnly.FromDateTime(
                x.CreatedAt.UtcDateTime) <= periodEnd);

        return new MergedPullRequestsResult(count);
    }
}