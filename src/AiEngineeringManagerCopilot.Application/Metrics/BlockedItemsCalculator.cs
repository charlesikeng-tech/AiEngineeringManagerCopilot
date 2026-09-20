using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class BlockedItemsCalculator
    : IBlockedItemsCalculator
{
    public BlockedItemsResult Calculate(
        IReadOnlyCollection<JiraWorkItem> workItems,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        if (periodStart > periodEnd)
        {
            throw new ArgumentException(
                "periodStart must be before or equal to periodEnd.");
        }

        var count = workItems.Count(x =>
            x.IsBlocked &&
            DateOnly.FromDateTime(
                x.CreatedAt.UtcDateTime) >= periodStart &&
            DateOnly.FromDateTime(
                x.CreatedAt.UtcDateTime) <= periodEnd);

        return new BlockedItemsResult(count);
    }
}