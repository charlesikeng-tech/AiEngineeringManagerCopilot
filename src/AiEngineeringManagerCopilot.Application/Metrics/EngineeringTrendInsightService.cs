using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class EngineeringTrendInsightService(
    EngineeringTrendSignalDetector signalDetector,
    EngineeringTrendInsightGenerator insightGenerator)
{
    public IReadOnlyList<EngineeringInsight> Generate(
        IReadOnlyList<MetricTrendResult> trends,
        IReadOnlyList<EngineeringInsight> existingInsights)
    {
        var existingMetricTypes = existingInsights
            .Select(x => x.MetricType)
            .ToHashSet();

        return trends
            .Where(x => !existingMetricTypes.Contains(x.MetricType))
            .Select(signalDetector.Detect)
            .Where(x => x is not null)
            .Select(x => insightGenerator.Generate(x!))
            .ToList();
    }
}