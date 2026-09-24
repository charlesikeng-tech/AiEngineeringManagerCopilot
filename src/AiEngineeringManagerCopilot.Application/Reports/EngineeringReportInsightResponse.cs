namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringReportInsightResponse(
    Guid Id,
    Guid ReportId,
    string MetricType,
    string Category,
    string Title,
    string Description,
    string Impact,
    string Recommendation);