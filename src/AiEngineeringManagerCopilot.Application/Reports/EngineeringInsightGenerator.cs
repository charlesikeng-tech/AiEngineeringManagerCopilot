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
                "Cycle time is above the healthy range",
                $"Average cycle time is {hours:F1} hours.",
                "The observed cycle time is above the configured healthy range and may indicate waiting time or oversized work.",
                "Review the cycle-time breakdown and identify the main sources of waiting time before deciding which improvements to prioritize."));
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
                "Pull request review time is above the healthy range",
                $"Average first review time is {hours:F1} hours.",
                "Longer review times may contribute to delivery delays, depending on the team's workflow and review practices.",
                "Review the causes of long review times and define a review-time expectation appropriate to the team's context."));
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

        if (count == 0)
        {
            insights.Add(
                new EngineeringInsight(
                    MetricType.DeploymentFrequency,
                    RiskCategory.Delivery,
                    "No deployments were recorded during the period",
                    "The system recorded 0 successful deployments during the selected period.",
                    "This may indicate a delivery bottleneck or incomplete deployment telemetry. " +
                    "The observed value alone does not establish that no deployments actually occurred.",
                    "Validate deployment telemetry and trace the path from merge to production " +
                    "before concluding that deployment activity is low."));

            return;
        }

        insights.Add(
            new EngineeringInsight(
                MetricType.DeploymentFrequency,
                RiskCategory.Delivery,
                "Recorded deployment frequency is below the healthy range",
                $"The system recorded {count:F0} successful deployments during the selected period.",
                "The observed deployment frequency is below the configured healthy range. " +
                "This may reflect delivery constraints, larger batches, or incomplete deployment telemetry.",
                "Validate deployment telemetry and review the delivery path before deciding which constraints to address."));
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
                "Change failure rate is above the healthy range",
                $"Change failure rate is {rate:F1}%.",
                "The observed failure rate is above the configured healthy range and may increase delivery risk and rework.",
                "Review failed changes and identify recurring causes before prioritizing improvements to testing, deployment safeguards, or monitoring."));
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
                "Lead time is above the healthy range",
                $"Average lead time is {hours:F1} hours.",
                "The observed lead time is above the configured healthy range and may reduce delivery predictability.",
                "Identify the longest waiting stages and investigate handoffs, queues, or dependencies contributing to the observed lead time."));
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
                "Open pull requests are above the healthy range",
                $"{count:F0} pull requests are currently open.",
                "The observed number of open pull requests is above the configured healthy range and may increase context switching or review delays.",
                "Review the open pull requests and identify whether any are waiting for review, blocked, stale, or no longer relevant."));
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
                "Recorded pull request throughput is below the healthy range",
                $"The system recorded {count:F0} merged pull requests during the selected period.",
                "The observed merge count is below the configured healthy range. " +
                "This may reflect delivery constraints, work-item size, team capacity, or the nature of work performed during the period.",
                "Review the delivery context and work-item flow before concluding that low merge volume represents a team performance issue."));
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
                "Blocked items are above the healthy range",
                $"{count:F0} items are currently recorded as blocked.",
                "The observed number of blocked items is above the configured healthy range and may reduce team flow.",
                "Review the blocked items, identify their causes and assign ownership for resolving the blockers that materially affect delivery."));
    }
}