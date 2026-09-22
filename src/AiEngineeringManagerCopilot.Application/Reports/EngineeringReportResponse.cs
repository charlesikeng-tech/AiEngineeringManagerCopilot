namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringReportResponse(
    Guid Id,
    Guid TeamId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string ExecutiveSummary,
    int OverallScore,
    string HealthLevel,
    decimal DataCoverage,
    DateTimeOffset CreatedAt,
    IReadOnlyList<EngineeringReportMetricResponse> Metrics,
    IReadOnlyList<EngineeringReportInsightResponse> Insights,
    IReadOnlyList<EngineeringActionResponse> Actions,
    IReadOnlyList<EngineeringReportRiskResponse> Risks,
    IReadOnlyList<EngineeringMetricTrendResponse> Trends);