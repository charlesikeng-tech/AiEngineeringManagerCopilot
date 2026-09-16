using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringReportMetricResponse(
    MetricType MetricType,
    decimal Value,
    int Score);