namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed record CycleTimeResult(
    decimal AverageHours,
    int PullRequestsCount);