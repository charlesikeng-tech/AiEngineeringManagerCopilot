using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class EngineeringMetricsService(
    ICurrentUser currentUser,
    ITeamRepository teamRepository,
    IPullRequestRepository pullRequestRepository,
    IJiraWorkItemRepository jiraWorkItemRepository,
    IPullRequestReviewRepository pullRequestReviewRepository,
    IEngineeringMetricRepository metricRepository,
    ICycleTimeCalculator cycleTimeCalculator,
    IPRReviewTimeCalculator prReviewTimeCalculator,
    IDeploymentRepository deploymentRepository,
    IDeploymentFrequencyCalculator deploymentFrequencyCalculator,
    IChangeFailureRateCalculator changeFailureRateCalculator,
    ILeadTimeCalculator leadTimeCalculator,
    IOpenPullRequestsCalculator openPullRequestsCalculator,
    IMergedPullRequestsCalculator mergedPullRequestsCalculator,
    IBlockedItemsCalculator blockedItemsCalculator,
    IGitHubConnectionRepository gitHubConnectionRepository,
    IJiraConnectionRepository jiraConnectionRepository)
    : IEngineeringMetricsService
{
    private static readonly IReadOnlyList<MetricType> AllMetricTypes =
    [
        MetricType.CycleTime,
        MetricType.PRReviewTime,
        MetricType.DeploymentFrequency,
        MetricType.ChangeFailureRate,
        MetricType.LeadTime,
        MetricType.OpenPRs,
        MetricType.MergedPRs,
        MetricType.BlockedItems
    ];

    public Task<IReadOnlyList<EngineeringMetricResponse>?> CalculateAllAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        CalculateAsync(
            teamId, periodStart, periodEnd, AllMetricTypes, cancellationToken);

    public Task<EngineeringMetricResponse?> CalculateCycleTimeAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        CalculateSingleAsync(
            teamId, periodStart, periodEnd, MetricType.CycleTime, cancellationToken);

    public Task<EngineeringMetricResponse?> CalculatePRReviewTimeAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        CalculateSingleAsync(
            teamId, periodStart, periodEnd, MetricType.PRReviewTime, cancellationToken);

    public Task<EngineeringMetricResponse?> CalculateDeploymentFrequencyAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        CalculateSingleAsync(
            teamId, periodStart, periodEnd, MetricType.DeploymentFrequency, cancellationToken);

    public Task<EngineeringMetricResponse?> CalculateChangeFailureRateAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        CalculateSingleAsync(
            teamId, periodStart, periodEnd, MetricType.ChangeFailureRate, cancellationToken);

    public Task<EngineeringMetricResponse?> CalculateLeadTimeAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        CalculateSingleAsync(
            teamId, periodStart, periodEnd, MetricType.LeadTime, cancellationToken);

    public Task<EngineeringMetricResponse?> CalculateOpenPullRequestsAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        CalculateSingleAsync(
            teamId, periodStart, periodEnd, MetricType.OpenPRs, cancellationToken);

    public Task<EngineeringMetricResponse?> CalculateMergedPullRequestsAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        CalculateSingleAsync(
            teamId, periodStart, periodEnd, MetricType.MergedPRs, cancellationToken);

    public Task<EngineeringMetricResponse?> CalculateBlockedItemsAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken) =>
        CalculateSingleAsync(
            teamId, periodStart, periodEnd, MetricType.BlockedItems, cancellationToken);

    private async Task<EngineeringMetricResponse?> CalculateSingleAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        MetricType metricType,
        CancellationToken cancellationToken)
    {
        var results = await CalculateAsync(
            teamId, periodStart, periodEnd, [metricType], cancellationToken);

        return results?[0];
    }

    private async Task<IReadOnlyList<EngineeringMetricResponse>?> CalculateAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyList<MetricType> metricTypes,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(periodStart, periodEnd);

        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var context = await LoadContextAsync(
            teamId, periodStart, periodEnd, metricTypes, cancellationToken);
        var results = new List<EngineeringMetricResponse>(metricTypes.Count);

        foreach (var metricType in metricTypes)
        {
            var (value, dataStatus) = CalculateMetric(
                metricType, context, periodStart, periodEnd);

            results.Add(await SaveMetricAsync(
                teamId,
                metricType,
                value,
                dataStatus,
                periodStart,
                periodEnd,
                cancellationToken));
        }

        return results;
    }

    private async Task<MetricCalculationContext> LoadContextAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyList<MetricType> metricTypes,
        CancellationToken cancellationToken)
    {
        var gitHubConfigured = metricTypes.Any(IsGitHubMetric) &&
            await IsGitHubConfiguredAsync(teamId, cancellationToken);
        var jiraConfigured = metricTypes.Any(type => !IsGitHubMetric(type)) &&
            await IsJiraConfiguredAsync(teamId, cancellationToken);

        IReadOnlyList<PullRequest> mergedPullRequests =
            gitHubConfigured && metricTypes.Contains(MetricType.CycleTime)
                ? await pullRequestRepository.GetMergedByTeamAndPeriodAsync(
                    teamId, periodStart, periodEnd, cancellationToken)
                : [];

        IReadOnlyList<PullRequest> periodPullRequests =
            gitHubConfigured &&
            (metricTypes.Contains(MetricType.PRReviewTime) ||
             metricTypes.Contains(MetricType.MergedPRs))
                ? await pullRequestRepository.GetByTeamAndPeriodAsync(
                    teamId, periodStart, periodEnd, cancellationToken)
                : [];

        IReadOnlyList<PullRequestReview> reviews =
            gitHubConfigured && metricTypes.Contains(MetricType.PRReviewTime)
                ? await pullRequestReviewRepository.GetByPullRequestIdsAsync(
                    periodPullRequests.Select(x => x.Id).ToArray(),
                    cancellationToken)
                : [];

        IReadOnlyList<Deployment> deployments =
            gitHubConfigured &&
            (metricTypes.Contains(MetricType.DeploymentFrequency) ||
             metricTypes.Contains(MetricType.ChangeFailureRate))
                ? await deploymentRepository.GetByTeamAndPeriodAsync(
                    teamId, periodStart, periodEnd, cancellationToken)
                : [];

        IReadOnlyList<JiraWorkItem> completedWorkItems =
            jiraConfigured && metricTypes.Contains(MetricType.LeadTime)
                ? await jiraWorkItemRepository.GetCompletedByTeamAndPeriodAsync(
                    teamId,
                    new DateTimeOffset(
                        periodStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                    new DateTimeOffset(
                        periodEnd.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero),
                    cancellationToken)
                : [];

        IReadOnlyList<PullRequest> openPullRequests =
            gitHubConfigured && metricTypes.Contains(MetricType.OpenPRs)
                ? await pullRequestRepository.GetOpenByTeamAtDateAsync(
                    teamId, periodEnd, cancellationToken)
                : [];

        IReadOnlyList<JiraWorkItem> workItems =
            jiraConfigured && metricTypes.Contains(MetricType.BlockedItems)
                ? await jiraWorkItemRepository.GetByTeamAsync(
                    teamId, cancellationToken)
                : [];

        return new MetricCalculationContext(
            gitHubConfigured,
            jiraConfigured,
            mergedPullRequests,
            periodPullRequests,
            reviews,
            deployments,
            completedWorkItems,
            openPullRequests,
            workItems);
    }

    private (decimal? Value, MetricDataStatus DataStatus) CalculateMetric(
        MetricType metricType,
        MetricCalculationContext context,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var sourceConfigured = IsGitHubMetric(metricType)
            ? context.GitHubConfigured
            : context.JiraConfigured;

        if (!sourceConfigured)
        {
            return (null, MetricDataStatus.SourceNotConfigured);
        }

        switch (metricType)
        {
            case MetricType.CycleTime:
                return context.MergedPullRequests.Count == 0
                    ? (null, MetricDataStatus.NoData)
                    : (cycleTimeCalculator.Calculate(
                        context.MergedPullRequests, periodStart, periodEnd).AverageHours,
                        MetricDataStatus.Available);

            case MetricType.PRReviewTime:
                var reviewTime = prReviewTimeCalculator.Calculate(
                    context.PeriodPullRequests, context.Reviews, periodStart, periodEnd);
                return reviewTime.PullRequestsCount == 0
                    ? (null, MetricDataStatus.NoData)
                    : (reviewTime.AverageHours, MetricDataStatus.Available);

            case MetricType.DeploymentFrequency:
                return (deploymentFrequencyCalculator.Calculate(
                    context.Deployments, periodStart, periodEnd).DeploymentCount,
                    MetricDataStatus.Available);

            case MetricType.ChangeFailureRate:
                return context.Deployments.Count == 0
                    ? (null, MetricDataStatus.NoData)
                    : (changeFailureRateCalculator.Calculate(
                        context.Deployments, periodStart, periodEnd).FailureRate,
                        MetricDataStatus.Available);

            case MetricType.LeadTime:
                return context.CompletedWorkItems.Count == 0
                    ? (null, MetricDataStatus.NoData)
                    : (leadTimeCalculator.Calculate(
                        context.CompletedWorkItems, periodStart, periodEnd).AverageHours,
                        MetricDataStatus.Available);

            case MetricType.OpenPRs:
                return (openPullRequestsCalculator.Calculate(
                    context.OpenPullRequests, periodStart, periodEnd).PullRequestsCount,
                    MetricDataStatus.Available);

            case MetricType.MergedPRs:
                return (mergedPullRequestsCalculator.Calculate(
                    context.PeriodPullRequests, periodStart, periodEnd).PullRequestsCount,
                    MetricDataStatus.Available);

            case MetricType.BlockedItems:
                return (blockedItemsCalculator.Calculate(
                    context.WorkItems, periodStart, periodEnd).ItemsCount,
                    MetricDataStatus.Available);

            default:
                throw new ArgumentOutOfRangeException(nameof(metricType), metricType, null);
        }
    }

    private static bool IsGitHubMetric(MetricType metricType) =>
        metricType switch
        {
            MetricType.CycleTime or
            MetricType.PRReviewTime or
            MetricType.DeploymentFrequency or
            MetricType.ChangeFailureRate or
            MetricType.OpenPRs or
            MetricType.MergedPRs => true,
            MetricType.LeadTime or MetricType.BlockedItems => false,
            _ => throw new ArgumentOutOfRangeException(nameof(metricType), metricType, null)
        };

    private async Task<bool> IsGitHubConfiguredAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var connection = await gitHubConnectionRepository.GetByTeamIdAsync(
            teamId, cancellationToken);

        return connection is not null;
    }

    private async Task<bool> IsJiraConfiguredAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var connection = await jiraConnectionRepository.GetByTeamIdAsync(
            teamId, cancellationToken);

        return connection is not null;
    }

    private async Task<EngineeringMetricResponse> SaveMetricAsync(
        Guid teamId,
        MetricType metricType,
        decimal? value,
        MetricDataStatus dataStatus,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var metric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = metricType,
            Value = value,
            DataStatus = dataStatus,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = now,
            UpdatedAt = now
        };

        var persistedMetric = await metricRepository.UpsertAsync(
            metric, cancellationToken);

        return new EngineeringMetricResponse(
            persistedMetric.Id,
            persistedMetric.TeamId,
            persistedMetric.MetricType,
            persistedMetric.Value,
            persistedMetric.DataStatus,
            persistedMetric.PeriodStart,
            persistedMetric.PeriodEnd,
            persistedMetric.CreatedAt);
    }

    private static void ValidatePeriod(DateOnly periodStart, DateOnly periodEnd)
    {
        if (periodStart > periodEnd)
        {
            throw new ArgumentException(
                "periodStart must be before or equal to periodEnd.");
        }
    }

    private sealed record MetricCalculationContext(
        bool GitHubConfigured,
        bool JiraConfigured,
        IReadOnlyList<PullRequest> MergedPullRequests,
        IReadOnlyList<PullRequest> PeriodPullRequests,
        IReadOnlyList<PullRequestReview> Reviews,
        IReadOnlyList<Deployment> Deployments,
        IReadOnlyList<JiraWorkItem> CompletedWorkItems,
        IReadOnlyList<PullRequest> OpenPullRequests,
        IReadOnlyList<JiraWorkItem> WorkItems);
}
