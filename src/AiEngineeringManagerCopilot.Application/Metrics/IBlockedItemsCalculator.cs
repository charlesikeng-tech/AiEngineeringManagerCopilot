using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public interface IBlockedItemsCalculator
{
    BlockedItemsResult Calculate(
        IReadOnlyCollection<JiraWorkItem> workItems,
        DateOnly periodStart,
        DateOnly periodEnd);
}