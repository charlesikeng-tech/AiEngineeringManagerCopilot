using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.AI;

public sealed record AIAnalysisContext(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int OverallScore,
    decimal DataCoverage,
    string ExecutiveSummary,
    IReadOnlyList<AIAnalysisMetricContext> Metrics,
    IReadOnlyList<EngineeringInsight> Insights,
    IReadOnlyList<EngineeringRisk> Risks,
    IReadOnlyList<MetricTrendResult> Trends);

public sealed record AIAnalysisMetricContext(
    MetricType MetricType,
    decimal? Value,
    MetricDataStatus DataStatus,
    string? Unit = null,
    MetricTemporalSemantics? TemporalSemantics = null,
    string? Description = null);