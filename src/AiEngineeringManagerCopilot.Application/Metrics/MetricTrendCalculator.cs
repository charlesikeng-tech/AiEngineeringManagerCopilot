using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class MetricTrendCalculator
{
    public MetricTrendResult Calculate(
        MetricType metricType,
        decimal currentValue,
        decimal previousValue)
    {
        decimal? changePercentage = previousValue == 0
            ? null
            : Math.Round(
                ((currentValue - previousValue) / previousValue) * 100m,
                2,
                MidpointRounding.AwayFromZero);

        var direction = GetDirection(
            metricType,
            currentValue,
            previousValue);

        return new MetricTrendResult(
            metricType,
            currentValue,
            previousValue,
            changePercentage,
            direction);
    }
    
    private static MetricTrendDirection GetDirection(
        MetricType metricType,
        decimal currentValue,
        decimal previousValue)
    {
        if (currentValue == previousValue)
            return MetricTrendDirection.Stable;

        var higherIsBetter = metricType is
            MetricType.DeploymentFrequency or
            MetricType.MergedPRs;

        if (higherIsBetter)
        {
            return currentValue > previousValue
                ? MetricTrendDirection.Improving
                : MetricTrendDirection.Degrading;
        }

        return currentValue < previousValue
            ? MetricTrendDirection.Improving
            : MetricTrendDirection.Degrading;
    }
}