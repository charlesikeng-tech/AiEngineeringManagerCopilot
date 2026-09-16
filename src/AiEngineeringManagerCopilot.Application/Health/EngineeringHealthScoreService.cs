using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Health;

public sealed class EngineeringHealthScoreService(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IEngineeringMetricRepository metricRepository,
    IEngineeringHealthScoreCalculator scoreCalculator)
    : IEngineeringHealthScoreService
{
    public async Task<EngineeringHealthScoreResponse?> CalculateAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var cycleTime = await GetMetricValueAsync(
            teamId,
            MetricType.CycleTime,
            periodStart,
            periodEnd,
            cancellationToken);

        var prReviewTime = await GetMetricValueAsync(
            teamId,
            MetricType.PRReviewTime,
            periodStart,
            periodEnd,
            cancellationToken);

        var deploymentFrequency = await GetMetricValueAsync(
            teamId,
            MetricType.DeploymentFrequency,
            periodStart,
            periodEnd,
            cancellationToken);

        var changeFailureRate = await GetMetricValueAsync(
            teamId,
            MetricType.ChangeFailureRate,
            periodStart,
            periodEnd,
            cancellationToken);

        var leadTime = await GetMetricValueAsync(
            teamId,
            MetricType.LeadTime,
            periodStart,
            periodEnd,
            cancellationToken);

        var openPullRequests = await GetMetricValueAsync(
            teamId,
            MetricType.OpenPRs,
            periodStart,
            periodEnd,
            cancellationToken);

        var mergedPullRequests = await GetMetricValueAsync(
            teamId,
            MetricType.MergedPRs,
            periodStart,
            periodEnd,
            cancellationToken);

        var blockedItems = await GetMetricValueAsync(
            teamId,
            MetricType.BlockedItems,
            periodStart,
            periodEnd,
            cancellationToken);

        var score = scoreCalculator.Calculate(
            cycleTimeHours: cycleTime,
            prReviewTimeHours: prReviewTime,
            deploymentCount: (int)deploymentFrequency,
            changeFailureRate: changeFailureRate,
            leadTimeHours: leadTime,
            openPullRequests: (int)openPullRequests,
            mergedPullRequests: (int)mergedPullRequests,
            blockedItems: (int)blockedItems);

        return new EngineeringHealthScoreResponse(
            teamId,
            periodStart,
            periodEnd,
            score.OverallScore,
            score.HealthLevel);
    }

    private async Task<decimal> GetMetricValueAsync(
        Guid teamId,
        MetricType metricType,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var metrics = await metricRepository.GetByTeamAndPeriodAsync(
            teamId,
            metricType,
            periodStart,
            periodEnd,
            cancellationToken);

        return metrics
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Value)
            .FirstOrDefault();
    }
}