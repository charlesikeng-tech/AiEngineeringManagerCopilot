using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class PRReviewTimeCalculator
    : IPRReviewTimeCalculator
{
    public PRReviewTimeResult Calculate(
        IReadOnlyCollection<PullRequest> pullRequests,
        IReadOnlyCollection<PullRequestReview> reviews,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        if (periodStart > periodEnd)
        {
            throw new ArgumentException(
                "periodStart must be before or equal to periodEnd.");
        }
        
        var durations = pullRequests
            .Where(x =>
                x.MergedAt.HasValue &&
                DateOnly.FromDateTime(
                    x.MergedAt.Value.UtcDateTime) >= periodStart &&
                DateOnly.FromDateTime(
                    x.MergedAt.Value.UtcDateTime) <= periodEnd)
            .Select(pullRequest =>
            {
                var firstReview = reviews
                    .Where(x =>
                        x.PullRequestId == pullRequest.Id &&
                        x.SubmittedAt >= pullRequest.CreatedAt)
                    .OrderBy(x => x.SubmittedAt)
                    .FirstOrDefault();

                if (firstReview is null)
                {
                    return (decimal?)null;
                }

                return (decimal)(
                        firstReview.SubmittedAt -
                        pullRequest.CreatedAt)
                    .TotalHours;
            })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Where(x => x >= 0)
            .ToList();

        if (durations.Count == 0)
        {
            return new PRReviewTimeResult(
                0,
                0);
        }

        return new PRReviewTimeResult(
            Math.Round(
                durations.Average(),
                2,
                MidpointRounding.AwayFromZero),
            durations.Count);
    }
}