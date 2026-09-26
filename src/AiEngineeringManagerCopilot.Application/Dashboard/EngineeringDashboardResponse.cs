using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Reports;

namespace AiEngineeringManagerCopilot.Application.Dashboard;

public sealed record EngineeringDashboardResponse(
    Guid TeamId,
    IReadOnlyList<EngineeringMetricResponse> Metrics,
    EngineeringHealthScoreResult HealthScore,
    IReadOnlyList<MetricTrendResult> Trends,
    IReadOnlyList<EngineeringDashboardRiskResponse> Risks,
    EngineeringReportResponse? LatestReport,
    AIAnalysisResult? AIAnalysis);