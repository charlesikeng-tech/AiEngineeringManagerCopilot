namespace AiEngineeringManagerCopilot.Application.Health;

public sealed record EngineeringHealthScoreResult(
    int OverallScore,
    string HealthLevel);