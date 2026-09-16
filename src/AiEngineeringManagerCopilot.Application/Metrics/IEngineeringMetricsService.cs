namespace AiEngineeringManagerCopilot.Application.Metrics;

public interface IEngineeringMetricsService
{
    Task<EngineeringMetricResponse?> CalculateCycleTimeAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
    
    Task<EngineeringMetricResponse?> CalculatePRReviewTimeAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
    
    Task<EngineeringMetricResponse?> CalculateDeploymentFrequencyAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
    
    Task<EngineeringMetricResponse?> CalculateChangeFailureRateAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
    
    Task<EngineeringMetricResponse?> CalculateLeadTimeAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
    
    Task<EngineeringMetricResponse?> CalculateOpenPullRequestsAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
    
    Task<EngineeringMetricResponse?> CalculateMergedPullRequestsAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
    
    Task<EngineeringMetricResponse?> CalculateBlockedItemsAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
}