namespace AiEngineeringManagerCopilot.Application.Health;

public sealed record EngineeringHealthScoreResponse(
    Guid TeamId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int OverallScore,
    string HealthLevel);