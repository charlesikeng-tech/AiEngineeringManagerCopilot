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
    public async Task<IReadOnlyList<EngineeringMetricResponse>?> CalculateAllAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
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

        var results = new List<EngineeringMetricResponse>();

        var cycleTime = await CalculateCycleTimeAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        var prReviewTime = await CalculatePRReviewTimeAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        var deploymentFrequency = await CalculateDeploymentFrequencyAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        var changeFailureRate = await CalculateChangeFailureRateAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        var leadTime = await CalculateLeadTimeAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        var openPullRequests = await CalculateOpenPullRequestsAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        var mergedPullRequests = await CalculateMergedPullRequestsAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        var blockedItems = await CalculateBlockedItemsAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        AddIfNotNull(results, cycleTime);
        AddIfNotNull(results, prReviewTime);
        AddIfNotNull(results, deploymentFrequency);
        AddIfNotNull(results, changeFailureRate);
        AddIfNotNull(results, leadTime);
        AddIfNotNull(results, openPullRequests);
        AddIfNotNull(results, mergedPullRequests);
        AddIfNotNull(results, blockedItems);

        return results;
    }

    public async Task<EngineeringMetricResponse?> CalculateCycleTimeAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(periodStart, periodEnd);

        if (!await TeamExistsAsync(teamId, cancellationToken))
        {
            return null;
        }

        if (!await IsGitHubConfiguredAsync(teamId, cancellationToken))
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.CycleTime,
                null,
                MetricDataStatus.SourceNotConfigured,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var pullRequests =
            await pullRequestRepository.GetMergedByTeamAndPeriodAsync(
                teamId,
                periodStart,
                periodEnd,
                cancellationToken);

        if (pullRequests.Count == 0)
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.CycleTime,
                null,
                MetricDataStatus.NoData,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var result = cycleTimeCalculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        return await SaveMetricAsync(
            teamId,
            MetricType.CycleTime,
            result.AverageHours,
            MetricDataStatus.Available,
            periodStart,
            periodEnd,
            cancellationToken);
    }

    public async Task<EngineeringMetricResponse?> CalculatePRReviewTimeAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(periodStart, periodEnd);

        if (!await TeamExistsAsync(teamId, cancellationToken))
        {
            return null;
        }

        if (!await IsGitHubConfiguredAsync(teamId, cancellationToken))
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.PRReviewTime,
                null,
                MetricDataStatus.SourceNotConfigured,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var pullRequests =
            await pullRequestRepository.GetByTeamAndPeriodAsync(
                teamId,
                periodStart,
                periodEnd,
                cancellationToken);

        var pullRequestIds = pullRequests
            .Select(x => x.Id)
            .ToArray();

        var reviews =
            await pullRequestReviewRepository.GetByPullRequestIdsAsync(
                pullRequestIds,
                cancellationToken);

        var result = prReviewTimeCalculator.Calculate(
            pullRequests,
            reviews,
            periodStart,
            periodEnd);

        if (result.PullRequestsCount == 0)
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.PRReviewTime,
                null,
                MetricDataStatus.NoData,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        return await SaveMetricAsync(
            teamId,
            MetricType.PRReviewTime,
            result.AverageHours,
            MetricDataStatus.Available,
            periodStart,
            periodEnd,
            cancellationToken);
    }

    public async Task<EngineeringMetricResponse?>
        CalculateDeploymentFrequencyAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        ValidatePeriod(periodStart, periodEnd);

        if (!await TeamExistsAsync(teamId, cancellationToken))
        {
            return null;
        }

        if (!await IsGitHubConfiguredAsync(teamId, cancellationToken))
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.DeploymentFrequency,
                null,
                MetricDataStatus.SourceNotConfigured,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var deployments =
            await deploymentRepository.GetByTeamAndPeriodAsync(
                teamId,
                periodStart,
                periodEnd,
                cancellationToken);

        var result = deploymentFrequencyCalculator.Calculate(
            deployments,
            periodStart,
            periodEnd);

        // 0 deployment est une valeur valide pour une source GitHub configurée.
        return await SaveMetricAsync(
            teamId,
            MetricType.DeploymentFrequency,
            result.DeploymentCount,
            MetricDataStatus.Available,
            periodStart,
            periodEnd,
            cancellationToken);
    }

    public async Task<EngineeringMetricResponse?>
        CalculateChangeFailureRateAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        ValidatePeriod(periodStart, periodEnd);

        if (!await TeamExistsAsync(teamId, cancellationToken))
        {
            return null;
        }

        if (!await IsGitHubConfiguredAsync(teamId, cancellationToken))
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.ChangeFailureRate,
                null,
                MetricDataStatus.SourceNotConfigured,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var deployments =
            await deploymentRepository.GetByTeamAndPeriodAsync(
                teamId,
                periodStart,
                periodEnd,
                cancellationToken);

        if (deployments.Count == 0)
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.ChangeFailureRate,
                null,
                MetricDataStatus.NoData,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var result = changeFailureRateCalculator.Calculate(
            deployments,
            periodStart,
            periodEnd);

        return await SaveMetricAsync(
            teamId,
            MetricType.ChangeFailureRate,
            result.FailureRate,
            MetricDataStatus.Available,
            periodStart,
            periodEnd,
            cancellationToken);
    }

    public async Task<EngineeringMetricResponse?> CalculateLeadTimeAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(periodStart, periodEnd);

        if (!await TeamExistsAsync(teamId, cancellationToken))
        {
            return null;
        }
        
        if (!await IsJiraConfiguredAsync(teamId, cancellationToken))
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.LeadTime,
                null,
                MetricDataStatus.SourceNotConfigured,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var from = new DateTimeOffset(
            periodStart.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var to = new DateTimeOffset(
            periodEnd.ToDateTime(TimeOnly.MaxValue),
            TimeSpan.Zero);

        var workItems =
            await jiraWorkItemRepository.GetCompletedByTeamAndPeriodAsync(
                teamId,
                from,
                to,
                cancellationToken);
        
        if (workItems.Count == 0)
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.LeadTime,
                null,
                MetricDataStatus.NoData,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var result = leadTimeCalculator.Calculate(
            workItems,
            periodStart,
            periodEnd);

        return await SaveMetricAsync(
            teamId,
            MetricType.LeadTime,
            result.AverageHours,
            MetricDataStatus.Available,
            periodStart,
            periodEnd,
            cancellationToken);
    }

    public async Task<EngineeringMetricResponse?>
        CalculateOpenPullRequestsAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        ValidatePeriod(periodStart, periodEnd);

        if (!await TeamExistsAsync(teamId, cancellationToken))
        {
            return null;
        }

        if (!await IsGitHubConfiguredAsync(teamId, cancellationToken))
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.OpenPRs,
                null,
                MetricDataStatus.SourceNotConfigured,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var pullRequests =
            await pullRequestRepository.GetOpenByTeamAtDateAsync(
                teamId,
                periodEnd,
                cancellationToken);

        var result = openPullRequestsCalculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        return await SaveMetricAsync(
            teamId,
            MetricType.OpenPRs,
            result.PullRequestsCount,
            MetricDataStatus.Available,
            periodStart,
            periodEnd,
            cancellationToken);
    }

    public async Task<EngineeringMetricResponse?>
        CalculateMergedPullRequestsAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        ValidatePeriod(periodStart, periodEnd);

        if (!await TeamExistsAsync(teamId, cancellationToken))
        {
            return null;
        }

        if (!await IsGitHubConfiguredAsync(teamId, cancellationToken))
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.MergedPRs,
                null,
                MetricDataStatus.SourceNotConfigured,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var pullRequests =
            await pullRequestRepository.GetByTeamAndPeriodAsync(
                teamId,
                periodStart,
                periodEnd,
                cancellationToken);

        var result = mergedPullRequestsCalculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        return await SaveMetricAsync(
            teamId,
            MetricType.MergedPRs,
            result.PullRequestsCount,
            MetricDataStatus.Available,
            periodStart,
            periodEnd,
            cancellationToken);
    }

    public async Task<EngineeringMetricResponse?>
        CalculateBlockedItemsAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        ValidatePeriod(periodStart, periodEnd);

        if (!await TeamExistsAsync(teamId, cancellationToken))
        {
            return null;
        }
        
        if (!await IsJiraConfiguredAsync(teamId, cancellationToken))
        {
            return await SaveMetricAsync(
                teamId,
                MetricType.BlockedItems,
                null,
                MetricDataStatus.SourceNotConfigured,
                periodStart,
                periodEnd,
                cancellationToken);
        }

        var workItems =
            await jiraWorkItemRepository.GetByTeamAsync(
                teamId,
                cancellationToken);

        var result = blockedItemsCalculator.Calculate(
            workItems,
            periodStart,
            periodEnd);

        return await SaveMetricAsync(
            teamId,
            MetricType.BlockedItems,
            result.ItemsCount,
            MetricDataStatus.Available,
            periodStart,
            periodEnd,
            cancellationToken);
    }

    private async Task<bool> TeamExistsAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        return team is not null;
    }

    private async Task<bool> IsGitHubConfiguredAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var connection =
            await gitHubConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        return connection is not null;
    }

    private async Task<bool> IsJiraConfiguredAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var connection =
            await jiraConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

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
        var metric =
            await metricRepository.GetByTeamAndPeriodAsync(
                teamId,
                metricType,
                periodStart,
                periodEnd,
                cancellationToken);

        if (metric is null)
        {
            metric = new EngineeringMetric
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                MetricType = metricType,
                Value = value,
                DataStatus = dataStatus,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await metricRepository.AddAsync(
                metric,
                cancellationToken);
        }
        else
        {
            metric.Value = value;
            metric.DataStatus = dataStatus;
            metric.CreatedAt = DateTimeOffset.UtcNow;
        }

        await metricRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringMetricResponse(
            metric.Id,
            metric.TeamId,
            metric.MetricType,
            metric.Value,
            metric.DataStatus,
            metric.PeriodStart,
            metric.PeriodEnd,
            metric.CreatedAt);
    }

    private static void AddIfNotNull(
        ICollection<EngineeringMetricResponse> results,
        EngineeringMetricResponse? metric)
    {
        if (metric is not null)
        {
            results.Add(metric);
        }
    }

    private static void ValidatePeriod(
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        if (periodStart > periodEnd)
        {
            throw new ArgumentException(
                "periodStart must be before or equal to periodEnd.");
        }
    }
}