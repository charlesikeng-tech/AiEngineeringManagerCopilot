using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.AI;

public sealed record AIAnalysisContext(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int OverallScore,
    string ExecutiveSummary,
    IReadOnlyDictionary<MetricType, decimal> Metrics,
    IReadOnlyList<EngineeringInsight> Insights,
    IReadOnlyList<EngineeringRisk> Risks);