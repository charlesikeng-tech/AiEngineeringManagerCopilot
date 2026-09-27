using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Risks;

public sealed record EngineeringRiskResponse(
    Guid Id,
    Guid ReportId,
    MetricType? MetricType,
    RiskSeverity Severity,
    RiskCategory Category,
    string Title,
    string Description,
    string Recommendation,
    DateTimeOffset CreatedAt);