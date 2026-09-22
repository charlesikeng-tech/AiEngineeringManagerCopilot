namespace AiEngineeringManagerCopilot.Application.Health;

public interface IEngineeringHealthScoreCalculator
{
    EngineeringHealthScoreResult Calculate(
        decimal? cycleTimeHours,
        decimal? prReviewTimeHours,
        int? deploymentCount,
        decimal? changeFailureRate,
        decimal? leadTimeHours,
        int? openPullRequests,
        int? mergedPullRequests,
        int? blockedItems);
}