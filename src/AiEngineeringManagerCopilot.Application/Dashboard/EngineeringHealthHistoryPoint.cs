namespace AiEngineeringManagerCopilot.Application.Dashboard;

public sealed record EngineeringHealthHistoryPoint(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int OverallScore,
    string HealthLevel,
    decimal DataCoverage);