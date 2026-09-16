using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringReportRiskResponse(
    Guid Id,
    Guid ReportId,
    RiskSeverity Severity,
    RiskCategory Category,
    string Title,
    string Description,
    string Recommendation,
    DateTimeOffset CreatedAt);