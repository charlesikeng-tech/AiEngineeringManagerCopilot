using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed record EngineeringTrendSignal(
    MetricType MetricType,
    MetricTrendDirection Direction,
    decimal? ChangePercentage);