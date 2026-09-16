using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Health;

public sealed class EngineeringMetricScoreCalculator
    : IEngineeringMetricScoreCalculator
{
    public int Calculate(
        MetricType metricType,
        decimal value)
    {
        return metricType switch
        {
            MetricType.CycleTime =>
                CalculateCycleTimeScore(value),

            MetricType.PRReviewTime =>
                CalculatePrReviewTimeScore(value),

            MetricType.DeploymentFrequency =>
                CalculateDeploymentFrequencyScore((int)value),

            MetricType.ChangeFailureRate =>
                CalculateChangeFailureRateScore(value),

            MetricType.LeadTime =>
                CalculateLeadTimeScore(value),

            MetricType.OpenPRs =>
                CalculateOpenPullRequestsScore((int)value),

            MetricType.MergedPRs =>
                CalculateMergedPullRequestsScore((int)value),

            MetricType.BlockedItems =>
                CalculateBlockedItemsScore((int)value),

            _ => throw new ArgumentOutOfRangeException(
                nameof(metricType),
                metricType,
                "Unsupported metric type.")
        };
    }

    private static int CalculateCycleTimeScore(decimal hours)
    {
        if (hours <= 8) return 100;
        if (hours <= 24) return 80;
        if (hours <= 48) return 60;
        if (hours <= 72) return 40;
        return 20;
    }

    private static int CalculatePrReviewTimeScore(decimal hours)
    {
        if (hours <= 4) return 100;
        if (hours <= 12) return 80;
        if (hours <= 24) return 60;
        if (hours <= 48) return 40;
        return 20;
    }

    private static int CalculateDeploymentFrequencyScore(int count)
    {
        if (count >= 20) return 100;
        if (count >= 10) return 80;
        if (count >= 5) return 60;
        if (count >= 1) return 40;
        return 20;
    }

    private static int CalculateChangeFailureRateScore(decimal rate)
    {
        if (rate <= 5) return 100;
        if (rate <= 10) return 80;
        if (rate <= 20) return 60;
        if (rate <= 30) return 40;
        return 20;
    }

    private static int CalculateLeadTimeScore(decimal hours)
    {
        if (hours <= 24) return 100;
        if (hours <= 48) return 80;
        if (hours <= 72) return 60;
        if (hours <= 168) return 40;
        return 20;
    }

    private static int CalculateOpenPullRequestsScore(int count)
    {
        if (count <= 2) return 100;
        if (count <= 5) return 80;
        if (count <= 10) return 60;
        if (count <= 20) return 40;
        return 20;
    }

    private static int CalculateMergedPullRequestsScore(int count)
    {
        if (count >= 20) return 100;
        if (count >= 10) return 80;
        if (count >= 5) return 60;
        if (count >= 1) return 40;
        return 20;
    }

    private static int CalculateBlockedItemsScore(int count)
    {
        if (count == 0) return 100;
        if (count <= 2) return 80;
        if (count <= 5) return 60;
        if (count <= 10) return 40;
        return 20;
    }
}