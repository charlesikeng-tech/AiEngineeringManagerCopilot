using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class EngineeringTrendInsightGenerator
{
    public EngineeringInsight Generate(
        EngineeringTrendSignal signal)
    {
        var change = signal.ChangePercentage.HasValue
            ? $"{Math.Abs(signal.ChangePercentage.Value):0.##}%"
            : "a significant change";

        return new EngineeringInsight(
            signal.MetricType,
            GetCategory(signal.MetricType),
            $"Early warning: {signal.MetricType} is degrading",
            $"{signal.MetricType} shows a degrading trend compared with the previous period ({change}).",
            "The metric is moving in an unfavorable direction and may become an engineering health issue if the trend continues.",
            $"Monitor {signal.MetricType} and investigate the causes of the degradation before it crosses a critical threshold.");
    }

    private static RiskCategory GetCategory(
        MetricType metricType)
    {
        return metricType switch
        {
            MetricType.CycleTime => RiskCategory.Delivery,
            MetricType.LeadTime => RiskCategory.Delivery,
            MetricType.MergedPRs => RiskCategory.Delivery,

            MetricType.PRReviewTime => RiskCategory.Review,
            MetricType.OpenPRs => RiskCategory.Review,

            MetricType.DeploymentFrequency => RiskCategory.Process,

            MetricType.ChangeFailureRate => RiskCategory.Reliability,

            MetricType.BlockedItems => RiskCategory.Process,

            _ => RiskCategory.Process
        };
    }
    
}