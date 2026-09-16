using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public interface IPRReviewTimeCalculator
{
    PRReviewTimeResult Calculate(
        IReadOnlyCollection<PullRequest> pullRequests,
        IReadOnlyCollection<PullRequestReview> reviews,
        DateOnly periodStart,
        DateOnly periodEnd);
}