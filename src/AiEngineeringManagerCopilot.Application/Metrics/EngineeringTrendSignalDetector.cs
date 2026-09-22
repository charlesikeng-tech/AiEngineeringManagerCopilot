using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class EngineeringTrendSignalDetector
{
    public EngineeringTrendSignal? Detect(
        MetricTrendResult trend)
    {
        if (trend.Direction != MetricTrendDirection.Degrading)
        {
            return null;
        }

        if (!trend.ChangePercentage.HasValue)
        {
            return new EngineeringTrendSignal(
                trend.MetricType,
                trend.Direction,
                null);
        }

        if (Math.Abs(trend.ChangePercentage.Value) <
            EngineeringTrendPolicy.EarlyWarningThresholdPercentage)
        {
            return null;
        }

        return new EngineeringTrendSignal(
            trend.MetricType,
            trend.Direction,
            trend.ChangePercentage);
    }
}