namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed record LeadTimeResult(
    decimal AverageHours,
    int ItemsCount);