using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class BlockedItemsCalculator
    : IBlockedItemsCalculator
{
    public BlockedItemsResult Calculate(
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
            x.IsBlocked &&
            DateOnly.FromDateTime(
                x.CreatedAt.UtcDateTime) >= periodStart &&
            DateOnly.FromDateTime(
                x.CreatedAt.UtcDateTime) <= periodEnd);

        return new BlockedItemsResult(count);
    }
}