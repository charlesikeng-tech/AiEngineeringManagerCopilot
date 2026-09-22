namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringMetricTrendResponse(
    string MetricType,
    decimal CurrentValue,
    decimal PreviousValue,
    decimal? ChangePercentage,
    string Direction);