using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class MetricTrendBuilder(
    MetricTrendCalculator trendCalculator)
{
    public IReadOnlyList<MetricTrendResult> Build(
        IReadOnlyDictionary<MetricType, decimal> currentMetrics,
        IReadOnlyDictionary<MetricType, decimal> previousMetrics)
    {
        var trends = new List<MetricTrendResult>();

        foreach (var (metricType, currentValue) in currentMetrics)
        {
            if (!previousMetrics.TryGetValue(
                    metricType,
                    out var previousValue))
            {
                continue;
            }

            trends.Add(
                trendCalculator.Calculate(
                    metricType,
                    currentValue,
                    previousValue));
        }

        return trends;
    }
}