using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class LeadTimeCalculator
    : ILeadTimeCalculator
{
    public LeadTimeResult Calculate(
        IReadOnlyCollection<JiraWorkItem> workItems,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        if (periodStart > periodEnd)
        {
            throw new ArgumentException(
                "periodStart must be before or equal to periodEnd.");
        }

        var durations = workItems
            .Where(x =>
                x.DoneAt.HasValue &&
                DateOnly.FromDateTime(
                    x.CreatedAt.UtcDateTime) >= periodStart &&
                DateOnly.FromDateTime(
                    x.CreatedAt.UtcDateTime) <= periodEnd &&
                x.DoneAt.Value >= x.CreatedAt)
            .Select(x =>
                (decimal)(
                    x.DoneAt!.Value - x.CreatedAt)
                .TotalHours)
            .ToList();

        if (durations.Count == 0)
        {
            return new LeadTimeResult(0, 0);
        }

        return new LeadTimeResult(
            Math.Round(
                durations.Average(),
                2,
                MidpointRounding.AwayFromZero),
            durations.Count);
    }
}