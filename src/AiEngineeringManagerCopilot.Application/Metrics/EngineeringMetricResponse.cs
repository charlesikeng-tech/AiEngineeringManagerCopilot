using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed record EngineeringMetricResponse(
    Guid Id,
    Guid TeamId,
    MetricType MetricType,
    decimal? Value,
    MetricDataStatus DataStatus,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateTimeOffset CreatedAt);