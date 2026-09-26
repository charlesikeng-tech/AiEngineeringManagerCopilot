using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Dashboard;

public sealed record EngineeringDashboardRiskResponse(
    Guid Id,
    Guid ReportId,
    MetricType? MetricType,
    RiskSeverity Severity,
    RiskCategory Category,
    string Title,
    string Description,
    string Recommendation,
    DateTimeOffset CreatedAt);