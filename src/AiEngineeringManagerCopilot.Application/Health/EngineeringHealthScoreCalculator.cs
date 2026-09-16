namespace AiEngineeringManagerCopilot.Application.Health;

public sealed class EngineeringHealthScoreCalculator
    : IEngineeringHealthScoreCalculator
{
    public EngineeringHealthScoreResult Calculate(
        decimal cycleTimeHours,
        decimal prReviewTimeHours,
        int deploymentCount,
        decimal changeFailureRate,
        decimal leadTimeHours,
        int openPullRequests,
        int mergedPullRequests,
        int blockedItems)
    {
        var cycleTimeScore = CalculateCycleTimeScore(cycleTimeHours);
        var prReviewTimeScore = CalculatePrReviewTimeScore(prReviewTimeHours);
        var deploymentFrequencyScore =
            CalculateDeploymentFrequencyScore(deploymentCount);
        var changeFailureRateScore =
            CalculateChangeFailureRateScore(changeFailureRate);
        var leadTimeScore = CalculateLeadTimeScore(leadTimeHours);
        var openPullRequestsScore =
            CalculateOpenPullRequestsScore(openPullRequests);
        var mergedPullRequestsScore =
            CalculateMergedPullRequestsScore(mergedPullRequests);
        var blockedItemsScore =
            CalculateBlockedItemsScore(blockedItems);

        var overallScore =
            cycleTimeScore * 0.20m +
            prReviewTimeScore * 0.15m +
            deploymentFrequencyScore * 0.15m +
            changeFailureRateScore * 0.15m +
            leadTimeScore * 0.15m +
            openPullRequestsScore * 0.05m +
            mergedPullRequestsScore * 0.05m +
            blockedItemsScore * 0.10m;

        var roundedScore =
            (int)Math.Round(
                overallScore,
                MidpointRounding.AwayFromZero);

        return new EngineeringHealthScoreResult(
            roundedScore,
            GetHealthLevel(roundedScore));
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

    private static string GetHealthLevel(int score)
    {
        if (score >= 90) return "Excellent";
        if (score >= 75) return "Healthy";
        if (score >= 60) return "Needs Attention";
        if (score >= 40) return "At Risk";

        return "Critical";
    }
}