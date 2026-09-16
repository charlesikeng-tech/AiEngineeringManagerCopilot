using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class CycleTimeCalculator
    : ICycleTimeCalculator
{
    public CycleTimeResult Calculate(
        IReadOnlyCollection<PullRequest> pullRequests,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var durations = pullRequests
            .Where(x =>
                x.MergedAt.HasValue &&
                DateOnly.FromDateTime(
                    x.CreatedAt.UtcDateTime) >= periodStart &&
                DateOnly.FromDateTime(
                    x.CreatedAt.UtcDateTime) <= periodEnd &&
                x.MergedAt.Value >= x.CreatedAt)
            .Select(x =>
                (decimal)(
                    x.MergedAt!.Value - x.CreatedAt)
                .TotalHours)
            .ToList();

        if (durations.Count == 0)
        {
            return new CycleTimeResult(
                0,
                0);
        }

        var averageHours =
            durations.Average();

        return new CycleTimeResult(
            Math.Round(
                averageHours,
                2,
                MidpointRounding.AwayFromZero),
            durations.Count);
    }
}