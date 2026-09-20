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
    IMergedPullRequestsCalculator  mergedPullRequestsCalculator,
    IBlockedItemsCalculator blockedItemsCalculator)
    : IEngineeringMetricsService
{
    public async Task<EngineeringMetricResponse?>
        CalculateCycleTimeAsync(
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

        var pullRequests =
            await pullRequestRepository
                .GetMergedByTeamAndPeriodAsync(
                    teamId,
                    periodStart,
                    periodEnd,
                    cancellationToken);

        var result = cycleTimeCalculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        var metric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.CycleTime,
            Value = result.AverageHours,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await metricRepository.AddAsync(
            metric,
            cancellationToken);

        await metricRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringMetricResponse(
            metric.Id,
            metric.TeamId,
            metric.MetricType,
            metric.Value,
            metric.PeriodStart,
            metric.PeriodEnd,
            metric.CreatedAt);
    }
    
    public async Task<EngineeringMetricResponse?>
        CalculatePRReviewTimeAsync(
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

        var pullRequests =
            await pullRequestRepository
                .GetByTeamAndPeriodAsync(
                    teamId,
                    periodStart,
                    periodEnd,
                    cancellationToken);

        var pullRequestIds =
            pullRequests
                .Select(x => x.Id)
                .ToArray();

        var reviews =
            await pullRequestReviewRepository
                .GetByPullRequestIdsAsync(
                    pullRequestIds,
                    cancellationToken);

        var result = prReviewTimeCalculator.Calculate(
            pullRequests,
            reviews,
            periodStart,
            periodEnd);

        var metric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.PRReviewTime,
            Value = result.AverageHours,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await metricRepository.AddAsync(
            metric,
            cancellationToken);

        await metricRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringMetricResponse(
            metric.Id,
            metric.TeamId,
            metric.MetricType,
            metric.Value,
            metric.PeriodStart,
            metric.PeriodEnd,
            metric.CreatedAt);
    }
    
    public async Task<EngineeringMetricResponse?>
        CalculateDeploymentFrequencyAsync(
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

        var deployments =
            await deploymentRepository.GetByTeamAndPeriodAsync(
                teamId,
                periodStart,
                periodEnd,
                cancellationToken);

        var result =
            deploymentFrequencyCalculator.Calculate(
                deployments,
                periodStart,
                periodEnd);

        var metric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.DeploymentFrequency,
            Value = result.DeploymentCount,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await metricRepository.AddAsync(
            metric,
            cancellationToken);

        await metricRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringMetricResponse(
            metric.Id,
            metric.TeamId,
            metric.MetricType,
            metric.Value,
            metric.PeriodStart,
            metric.PeriodEnd,
            metric.CreatedAt);
    }

    public async Task<EngineeringMetricResponse?>
        CalculateChangeFailureRateAsync(
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

        var deployments =
            await deploymentRepository.GetByTeamAndPeriodAsync(
                teamId,
                periodStart,
                periodEnd,
                cancellationToken);

        var result =
            changeFailureRateCalculator.Calculate(
                deployments,
                periodStart,
                periodEnd);

        var metric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.ChangeFailureRate,
            Value = result.FailureRate,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await metricRepository.AddAsync(
            metric,
            cancellationToken);

        await metricRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringMetricResponse(
            metric.Id,
            metric.TeamId,
            metric.MetricType,
            metric.Value,
            metric.PeriodStart,
            metric.PeriodEnd,
            metric.CreatedAt);
    }
    
    public async Task<EngineeringMetricResponse?> CalculateLeadTimeAsync(
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
            return null;

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

        var result = leadTimeCalculator.Calculate(
            workItems,
            periodStart,
            periodEnd);

        var metric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.LeadTime,
            Value = result.AverageHours,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await metricRepository.AddAsync(
            metric,
            cancellationToken);

        await metricRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringMetricResponse(
            metric.Id,
            metric.TeamId,
            metric.MetricType,
            metric.Value,
            metric.PeriodStart,
            metric.PeriodEnd,
            metric.CreatedAt);
    }
    
    public async Task<EngineeringMetricResponse?>
        CalculateOpenPullRequestsAsync(
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
            return null;

        var pullRequests =
            await pullRequestRepository.GetByTeamAndPeriodAsync(
                teamId,
                periodStart,
                periodEnd,
                cancellationToken);

        var result = openPullRequestsCalculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        var metric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.OpenPRs,
            Value = result.PullRequestsCount,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await metricRepository.AddAsync(
            metric,
            cancellationToken);

        await metricRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringMetricResponse(
            metric.Id,
            metric.TeamId,
            metric.MetricType,
            metric.Value,
            metric.PeriodStart,
            metric.PeriodEnd,
            metric.CreatedAt);
    }
    
    public async Task<EngineeringMetricResponse?>
        CalculateMergedPullRequestsAsync(
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
            return null;

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

        var metric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.MergedPRs,
            Value = result.PullRequestsCount,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await metricRepository.AddAsync(
            metric,
            cancellationToken);

        await metricRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringMetricResponse(
            metric.Id,
            metric.TeamId,
            metric.MetricType,
            metric.Value,
            metric.PeriodStart,
            metric.PeriodEnd,
            metric.CreatedAt);
    }

    public async Task<EngineeringMetricResponse?>
        CalculateBlockedItemsAsync(
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
            return null;

        var from = new DateTimeOffset(
            periodStart.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var to = new DateTimeOffset(
            periodEnd.ToDateTime(TimeOnly.MaxValue),
            TimeSpan.Zero);

        var workItems =
            await jiraWorkItemRepository.GetByTeamAndPeriodAsync(
                teamId,
                from,
                to,
                cancellationToken);

        var result = blockedItemsCalculator.Calculate(
            workItems,
            periodStart,
            periodEnd);

        var metric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.BlockedItems,
            Value = result.ItemsCount,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await metricRepository.AddAsync(
            metric,
            cancellationToken);

        await metricRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringMetricResponse(
            metric.Id,
            metric.TeamId,
            metric.MetricType,
            metric.Value,
            metric.PeriodStart,
            metric.PeriodEnd,
            metric.CreatedAt);
    }
}