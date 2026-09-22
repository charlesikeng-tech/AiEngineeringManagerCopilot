using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringInsight(
    MetricType? MetricType,
    RiskCategory Category,
    string Title,
    string Description,
    string Impact,
    string Recommendation);