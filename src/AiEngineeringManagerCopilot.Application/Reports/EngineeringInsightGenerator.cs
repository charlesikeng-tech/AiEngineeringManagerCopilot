using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed class EngineeringInsightGenerator
    : IEngineeringInsightGenerator
{
    public IReadOnlyList<EngineeringInsight> Generate(
        IReadOnlyDictionary<MetricType, decimal> metrics)
    {
        var insights = new List<EngineeringInsight>();

        AddCycleTimeInsight(metrics, insights);
        AddReviewTimeInsight(metrics, insights);
        AddDeploymentFrequencyInsight(metrics, insights);
        AddChangeFailureRateInsight(metrics, insights);
        AddLeadTimeInsight(metrics, insights);
        AddOpenPullRequestsInsight(metrics, insights);
        AddMergedPullRequestsInsight(metrics, insights);
        AddBlockedItemsInsight(metrics, insights);

        return insights;
    }

    private static void AddCycleTimeInsight(
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringInsight> insights)
    {
        if (!metrics.TryGetValue(
                MetricType.CycleTime,
                out var hours))
        {
            return;
        }

        if (hours <= EngineeringHealthPolicy.CycleTime.HealthyMax)
        {
            return;
        }

        insights.Add(
            new EngineeringInsight(
                MetricType.CycleTime,
                RiskCategory.Delivery,
                "Cycle time is too high",
                $"Average cycle time is {hours:F1} hours.",
                "Work takes longer than expected to move from creation to completion.",
                "Break down large pull requests and identify the main sources of waiting time."));
    }

    private static void AddReviewTimeInsight(
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringInsight> insights)
    {
        if (!metrics.TryGetValue(
                MetricType.PRReviewTime,
                out var hours))
        {
            return;
        }

        if (hours <= EngineeringHealthPolicy.PrReviewTime.HealthyMax)
        {
            return;
        }

        insights.Add(
            new EngineeringInsight(
                MetricType.PRReviewTime,
                RiskCategory.Review,
                "Pull request review time is high",
                $"Average first review time is {hours:F1} hours.",
                "Slow reviews can create delivery bottlenecks.",
                "Define a review-time target and make pull request ownership explicit."));
    }

    private static void AddDeploymentFrequencyInsight(
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringInsight> insights)
    {
        if (!metrics.TryGetValue(
                MetricType.DeploymentFrequency,
                out var count))
        {
            return;
        }

        if (count >= EngineeringHealthPolicy.DeploymentFrequency.HealthyMin)
        {
            return;
        }

        insights.Add(
            new EngineeringInsight(
                MetricType.DeploymentFrequency,
                RiskCategory.Delivery,
                "Deployment frequency is low",
                $"Only {count:F0} successful deployments were recorded.",
                "Low deployment frequency can indicate large batches or release friction.",
                "Reduce batch size and identify the main constraints preventing frequent releases."));
    }

    private static void AddChangeFailureRateInsight(
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringInsight> insights)
    {
        if (!metrics.TryGetValue(
                MetricType.ChangeFailureRate,
                out var rate))
        {
            return;
        }

        if (rate <= EngineeringHealthPolicy.ChangeFailureRate.HealthyMax)
        {
            return;
        }

        insights.Add(
            new EngineeringInsight(
                MetricType.ChangeFailureRate,
                RiskCategory.Quality,
                "Change failure rate is high",
                $"Change failure rate is {rate:F1}%.",
                "A high failure rate indicates increased delivery risk and rework.",
                "Strengthen automated testing, deployment safeguards and post-deployment monitoring."));
    }

    private static void AddLeadTimeInsight(
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringInsight> insights)
    {
        if (!metrics.TryGetValue(
                MetricType.LeadTime,
                out var hours))
        {
            return;
        }

        if (hours <= EngineeringHealthPolicy.LeadTime.HealthyMax)
        {
            return;
        }

        insights.Add(
            new EngineeringInsight(
                MetricType.LeadTime,
                RiskCategory.Delivery,
                "Lead time is high",
                $"Average lead time is {hours:F1} hours.",
                "Long lead times reduce delivery predictability.",
                "Identify the longest waiting stages and reduce handoffs and queue time."));
    }

    private static void AddOpenPullRequestsInsight(
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringInsight> insights)
    {
        if (!metrics.TryGetValue(
                MetricType.OpenPRs,
                out var count))
        {
            return;
        }

        if (count <= EngineeringHealthPolicy.OpenPullRequests.HealthyMax)
        {
            return;
        }

        insights.Add(
            new EngineeringInsight(
                MetricType.OpenPRs,
                RiskCategory.Review,
                "Too many pull requests are open",
                $"{count:F0} pull requests are currently open.",
                "A large PR backlog can increase context switching and review delays.",
                "Prioritize existing pull requests before starting additional work."));
    }

    private static void AddMergedPullRequestsInsight(
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringInsight> insights)
    {
        if (!metrics.TryGetValue(
                MetricType.MergedPRs,
                out var count))
        {
            return;
        }

        if (count >= EngineeringHealthPolicy.MergedPullRequests.HealthyMin)
        {
            return;
        }

        insights.Add(
            new EngineeringInsight(
                MetricType.MergedPRs,
                RiskCategory.Delivery,
                "Low pull request throughput",
                $"Only {count:F0} pull requests were merged.",
                "Low throughput can indicate delivery bottlenecks or oversized work items.",
                "Review work-item size and identify the main constraints limiting throughput."));
    }

    private static void AddBlockedItemsInsight(
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringInsight> insights)
    {
        if (!metrics.TryGetValue(
                MetricType.BlockedItems,
                out var count))
        {
            return;
        }

        if (count <= EngineeringHealthPolicy.BlockedItems.HealthyMax)
        {
            return;
        }

        insights.Add(
            new EngineeringInsight(
                MetricType.BlockedItems,
                RiskCategory.Process,
                "Too many items are blocked",
                $"{count:F0} items are currently blocked.",
                "Blocked work increases delivery risk and reduces team flow.",
                "Review blocked items during team rituals and assign an explicit owner to each blocker."));
    }
}