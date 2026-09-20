using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class OpenPullRequestsCalculator
    : IOpenPullRequestsCalculator
{
    public OpenPullRequestsResult Calculate(
        IReadOnlyCollection<PullRequest> pullRequests,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        if (periodStart > periodEnd)
        {
            throw new ArgumentException(
                "periodStart must be before or equal to periodEnd.");
        }

        var end = new DateTimeOffset(
            periodEnd
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var count = pullRequests.Count(x =>
            x.CreatedAt < end &&
            (
                !x.ClosedAt.HasValue ||
                x.ClosedAt.Value >= end
            ));

        return new OpenPullRequestsResult(count);
    }
}