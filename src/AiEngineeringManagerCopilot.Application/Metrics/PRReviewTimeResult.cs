namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed record PRReviewTimeResult(
    decimal AverageHours,
    int PullRequestsCount);