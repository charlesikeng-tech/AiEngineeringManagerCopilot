using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Risks;

public sealed class EngineeringRiskDetector : IEngineeringRiskDetector
{
    public IReadOnlyList<EngineeringRisk> Detect(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics)
    {
        var risks = new List<EngineeringRisk>();

        DetectCycleTimeRisk(teamId, reportId, metrics, risks);
        DetectReviewTimeRisk(teamId, reportId, metrics, risks);
        DetectDeploymentFrequencyRisk(teamId, reportId, metrics, risks);
        DetectChangeFailureRateRisk(teamId, reportId, metrics, risks);
        DetectLeadTimeRisk(teamId, reportId, metrics, risks);
        DetectOpenPullRequestsRisk(teamId, reportId, metrics, risks);
        DetectMergedPullRequestsRisk(teamId, reportId, metrics, risks);
        DetectBlockedItemsRisk(teamId, reportId, metrics, risks);

        return risks;
    }

    private static void DetectCycleTimeRisk(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringRisk> risks)
    {
        if (!metrics.TryGetValue(MetricType.CycleTime, out var value) ||
            value <= EngineeringHealthPolicy.CycleTime.NeedsAttentionMax)
        {
            return;
        }

        risks.Add(CreateRisk(
            teamId,
            reportId,
            MetricType.CycleTime,
            RiskSeverity.High,
            RiskCategory.Delivery,
            "High cycle time",
            $"Average cycle time is {value:0.##} hours.",
            "Reduce work in progress and split large pull requests."));
    }

    private static void DetectReviewTimeRisk(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringRisk> risks)
    {
        if (!metrics.TryGetValue(MetricType.PRReviewTime, out var value) ||
            value <= EngineeringHealthPolicy.PrReviewTime.NeedsAttentionMax)
        {
            return;
        }

        risks.Add(CreateRisk(
            teamId,
            reportId,
            MetricType.PRReviewTime,
            RiskSeverity.High,
            RiskCategory.Review,
            "Slow pull request reviews",
            $"Average PR review time is {value:0.##} hours.",
            "Set a review SLA and prioritize reviewing open pull requests."));
    }

    private static void DetectDeploymentFrequencyRisk(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringRisk> risks)
    {
        if (!metrics.TryGetValue(MetricType.DeploymentFrequency, out var value) ||
            value >= EngineeringHealthPolicy.DeploymentFrequency.NeedsAttentionMin)
        {
            return;
        }

        risks.Add(CreateRisk(
            teamId,
            reportId,
            MetricType.DeploymentFrequency,
            RiskSeverity.High,
            RiskCategory.Delivery,
            "Low deployment frequency",
            $"The team deployed only {value:0.##} times during the period.",
            "Reduce batch size and automate the path to production."));
    }

    private static void DetectChangeFailureRateRisk(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringRisk> risks)
    {
        if (!metrics.TryGetValue(MetricType.ChangeFailureRate, out var value) ||
            value <= EngineeringHealthPolicy.ChangeFailureRate.NeedsAttentionMax)
        {
            return;
        }

        risks.Add(CreateRisk(
            teamId,
            reportId,
            MetricType.ChangeFailureRate,
            RiskSeverity.Critical,
            RiskCategory.Reliability,
            "High change failure rate",
            $"Change failure rate is {value:0.##}%.",
            "Strengthen automated tests, deployment validation and rollback mechanisms."));
    }

    private static void DetectLeadTimeRisk(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringRisk> risks)
    {
        if (!metrics.TryGetValue(MetricType.LeadTime, out var value) ||
            value <= EngineeringHealthPolicy.LeadTime.NeedsAttentionMax)
        {
            return;
        }

        risks.Add(CreateRisk(
            teamId,
            reportId,
            MetricType.LeadTime,
            RiskSeverity.High,
            RiskCategory.Delivery,
            "High lead time",
            $"Average lead time is {value:0.##} hours.",
            "Reduce waiting time between development, review and deployment."));
    }

    private static void DetectOpenPullRequestsRisk(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringRisk> risks)
    {
        if (!metrics.TryGetValue(MetricType.OpenPRs, out var value) ||
            value <= EngineeringHealthPolicy.OpenPullRequests.NeedsAttentionMax)
        {
            return;
        }

        risks.Add(CreateRisk(
            teamId,
            reportId,
            MetricType.OpenPRs,
            RiskSeverity.Medium,
            RiskCategory.Review,
            "Too many open pull requests",
            $"There are {value:0.##} open pull requests.",
            "Prioritize existing pull requests before starting new work."));
    }

    private static void DetectMergedPullRequestsRisk(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringRisk> risks)
    {
        if (!metrics.TryGetValue(MetricType.MergedPRs, out var value) ||
            value >= EngineeringHealthPolicy.MergedPullRequests.NeedsAttentionMin)
        {
            return;
        }

        risks.Add(CreateRisk(
            teamId,
            reportId,
            MetricType.MergedPRs,
            RiskSeverity.High,
            RiskCategory.Delivery,
            "Low delivery throughput",
            $"Only {value:0.##} pull requests were merged during the period.",
            "Identify delivery bottlenecks and reduce work in progress."));
    }

    private static void DetectBlockedItemsRisk(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        List<EngineeringRisk> risks)
    {
        if (!metrics.TryGetValue(MetricType.BlockedItems, out var value) ||
            value <= EngineeringHealthPolicy.BlockedItems.NeedsAttentionMax)
        {
            return;
        }

        risks.Add(CreateRisk(
            teamId,
            reportId,
            MetricType.BlockedItems,
            RiskSeverity.High,
            RiskCategory.Process,
            "Too many blocked items",
            $"There are {value:0.##} blocked items.",
            "Identify blockers, assign owners and track them until resolution."));
    }

    private static EngineeringRisk CreateRisk(
        Guid teamId,
        Guid reportId,
        MetricType metricType,
        RiskSeverity severity,
        RiskCategory category,
        string title,
        string description,
        string recommendation)
    {
        return new EngineeringRisk
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ReportId = reportId,
            MetricType = metricType,
            Severity = severity,
            Category = category,
            Title = title,
            Description = description,
            Recommendation = recommendation,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}