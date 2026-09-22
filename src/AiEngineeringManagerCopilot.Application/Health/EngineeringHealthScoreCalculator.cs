namespace AiEngineeringManagerCopilot.Application.Health;

public sealed class EngineeringHealthScoreCalculator
    : IEngineeringHealthScoreCalculator
{
public EngineeringHealthScoreResult Calculate(
    decimal? cycleTimeHours,
    decimal? prReviewTimeHours,
    int? deploymentCount,
    decimal? changeFailureRate,
    decimal? leadTimeHours,
    int? openPullRequests,
    int? mergedPullRequests,
    int? blockedItems)
{
    decimal weightedScore = 0;
    decimal availableWeight = 0;

    if (cycleTimeHours.HasValue)
    {
        weightedScore +=
            CalculateCycleTimeScore(cycleTimeHours.Value) * 0.20m;
        availableWeight += 0.20m;
    }

    if (prReviewTimeHours.HasValue)
    {
        weightedScore +=
            CalculatePrReviewTimeScore(prReviewTimeHours.Value) * 0.15m;
        availableWeight += 0.15m;
    }

    if (deploymentCount.HasValue)
    {
        weightedScore +=
            CalculateDeploymentFrequencyScore(deploymentCount.Value) * 0.15m;
        availableWeight += 0.15m;
    }

    if (changeFailureRate.HasValue)
    {
        weightedScore +=
            CalculateChangeFailureRateScore(changeFailureRate.Value) * 0.15m;
        availableWeight += 0.15m;
    }

    if (leadTimeHours.HasValue)
    {
        weightedScore +=
            CalculateLeadTimeScore(leadTimeHours.Value) * 0.15m;
        availableWeight += 0.15m;
    }

    if (openPullRequests.HasValue)
    {
        weightedScore +=
            CalculateOpenPullRequestsScore(openPullRequests.Value) * 0.05m;
        availableWeight += 0.05m;
    }

    if (mergedPullRequests.HasValue)
    {
        weightedScore +=
            CalculateMergedPullRequestsScore(mergedPullRequests.Value) * 0.05m;
        availableWeight += 0.05m;
    }

    if (blockedItems.HasValue)
    {
        weightedScore +=
            CalculateBlockedItemsScore(blockedItems.Value) * 0.10m;
        availableWeight += 0.10m;
    }

    if (availableWeight == 0)
    {
        return new EngineeringHealthScoreResult(
            0,
            "No Data",
            0m);
    }

    var overallScore =
        weightedScore / availableWeight;

    var roundedScore =
        (int)Math.Round(
            overallScore,
            MidpointRounding.AwayFromZero);

    var dataCoverage =
        Math.Round(
            availableWeight * 100m,
            2,
            MidpointRounding.AwayFromZero);

    return new EngineeringHealthScoreResult(
        roundedScore,
        EngineeringHealthLevelResolver.Resolve(
            roundedScore,
            dataCoverage),
        dataCoverage);
}

    private static int CalculateCycleTimeScore(decimal hours)
    {
        if (hours <= EngineeringHealthPolicy.CycleTime.ExcellentMax) return 100;
        if (hours <= EngineeringHealthPolicy.CycleTime.HealthyMax) return 80;
        if (hours <= EngineeringHealthPolicy.CycleTime.NeedsAttentionMax) return 60;
        if (hours <= EngineeringHealthPolicy.CycleTime.AtRiskMax) return 40;

        return 20;
    }

    private static int CalculatePrReviewTimeScore(decimal hours)
    {
        if (hours <= EngineeringHealthPolicy.PrReviewTime.ExcellentMax) return 100;
        if (hours <= EngineeringHealthPolicy.PrReviewTime.HealthyMax) return 80;
        if(hours <= EngineeringHealthPolicy.PrReviewTime.NeedsAttentionMax) return 60;
        if(hours <= EngineeringHealthPolicy.PrReviewTime.AtRiskMax) return 40;
        
        return 20;
    }

    private static int CalculateDeploymentFrequencyScore(int count)
    {
        if (count >= EngineeringHealthPolicy.DeploymentFrequency.ExcellentMin) return 100;
        if (count >= EngineeringHealthPolicy.DeploymentFrequency.HealthyMin) return 80;
        if (count >= EngineeringHealthPolicy.DeploymentFrequency.NeedsAttentionMin) return 60;
        if (count >= EngineeringHealthPolicy.DeploymentFrequency.AtRiskMin) return 40;

        return 20;
    }

    private static int CalculateChangeFailureRateScore(decimal rate)
    {
        if (rate <= EngineeringHealthPolicy.ChangeFailureRate.ExcellentMax) return 100;
        if (rate <= EngineeringHealthPolicy.ChangeFailureRate.HealthyMax) return 80;
        if(rate <= EngineeringHealthPolicy.ChangeFailureRate.NeedsAttentionMax) return 60;
        if(rate <= EngineeringHealthPolicy.ChangeFailureRate.AtRiskMax) return 40;

        return 20;
    }

    private static int CalculateLeadTimeScore(decimal hours)
    {
        if (hours <= EngineeringHealthPolicy.LeadTime.ExcellentMax) return 100;
        if (hours <= EngineeringHealthPolicy.LeadTime.HealthyMax) return 80;
        if(hours <= EngineeringHealthPolicy.LeadTime.NeedsAttentionMax) return 60;
        if(hours <= EngineeringHealthPolicy.LeadTime.AtRiskMax) return 40;

        return 20;
    }

    private static int CalculateOpenPullRequestsScore(int count)
    {
        if (count <= EngineeringHealthPolicy.OpenPullRequests.ExcellentMax) return 100;
        if (count <= EngineeringHealthPolicy.OpenPullRequests.HealthyMax) return 80;
        if (count <= EngineeringHealthPolicy.OpenPullRequests.NeedsAttentionMax) return 60;
        if (count <= EngineeringHealthPolicy.OpenPullRequests.AtRiskMax) return 40;

        return 20;
    }

    private static int CalculateMergedPullRequestsScore(int count)
    {
        if (count >= EngineeringHealthPolicy.MergedPullRequests.ExcellentMin) return 100;
        if (count >= EngineeringHealthPolicy.MergedPullRequests.HealthyMin) return 80;
        if (count >= EngineeringHealthPolicy.MergedPullRequests.NeedsAttentionMin) return 60;
        if (count >= EngineeringHealthPolicy.MergedPullRequests.AtRiskMin) return 40;

        return 20;
    }

    private static int CalculateBlockedItemsScore(int count)
    {
        if (count == EngineeringHealthPolicy.BlockedItems.ExcellentMax) return 100;
        if (count <= EngineeringHealthPolicy.BlockedItems.HealthyMax) return 80;
        if (count <= EngineeringHealthPolicy.BlockedItems.NeedsAttentionMax) return 60;
        if (count <= EngineeringHealthPolicy.BlockedItems.AtRiskMax) return 40;

        return 20;
    }
    
}