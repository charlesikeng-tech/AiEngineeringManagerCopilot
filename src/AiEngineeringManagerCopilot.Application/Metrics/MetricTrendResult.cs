using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed record MetricTrendResult(
    MetricType MetricType,
    decimal CurrentValue,
    decimal PreviousValue,
    decimal? ChangePercentage,
    MetricTrendDirection Direction);