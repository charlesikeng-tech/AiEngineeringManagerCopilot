using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Actions;
using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed class EngineeringReportService(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IEngineeringHealthScoreCalculator healthScoreCalculator,
    IEngineeringReportRepository reportRepository,
    IEngineeringMetricRepository metricRepository,
    IEngineeringInsightGenerator insightGenerator,
    IEngineeringReportInsightRepository insightRepository,
    IEngineeringActionRepository actionRepository,
    IEngineeringActionGenerator actionGenerator,
    IEngineeringMetricScoreCalculator metricScoreCalculator,
    IEngineeringRiskDetector riskDetector,
    IEngineeringRiskRepository riskRepository,
    PreviousPeriodCalculator previousPeriodCalculator,
    MetricTrendBuilder metricTrendBuilder,
    EngineeringTrendInsightService trendInsightService)
    : IEngineeringReportService
{
    private readonly EngineeringTrendInsightService trendInsightService =
        trendInsightService;
    
    private readonly MetricTrendBuilder metricTrendBuilder =
        metricTrendBuilder;
    
    private readonly PreviousPeriodCalculator previousPeriodCalculator =
        previousPeriodCalculator;
    
    public async Task<EngineeringReportResponse?> GenerateAsync(
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

        var sourceMetrics = await metricRepository.GetByTeamAndPeriodAsync(
            teamId, periodStart, periodEnd, cancellationToken);
        var metrics = sourceMetrics
            .Where(x => x.DataStatus == MetricDataStatus.Available && x.Value.HasValue)
            .ToDictionary(x => x.MetricType, x => x.Value!.Value);
        decimal? Value(MetricType type) => metrics.TryGetValue(type, out var value) ? value : null;
        var healthScore = healthScoreCalculator.Calculate(
            Value(MetricType.CycleTime), Value(MetricType.PRReviewTime),
            (int?)Value(MetricType.DeploymentFrequency), Value(MetricType.ChangeFailureRate),
            Value(MetricType.LeadTime), (int?)Value(MetricType.OpenPRs),
            (int?)Value(MetricType.MergedPRs), (int?)Value(MetricType.BlockedItems));

        var trends = await GetTrendsAsync(
            teamId,
            periodStart,
            periodEnd,
            metrics,
            cancellationToken);

        var generatedInsights = insightGenerator.Generate(metrics);
        
        var report = new EngineeringReport
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            ExecutiveSummary = BuildExecutiveSummary(
                healthScore.OverallScore,
                healthScore.HealthLevel),
            OverallScore = healthScore.OverallScore,
            DataCoverage = healthScore.DataCoverage,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        var trendInsights = trendInsightService.Generate(
            trends,
            generatedInsights);
        
        var allInsights = generatedInsights
            .Concat(trendInsights)
            .ToList();
        
        var generatedRisks = riskDetector.Detect(
            teamId,
            report.Id,
            metrics);
        
        var generatedActions = actionGenerator.Generate(
            allInsights,
            generatedRisks);
        
        var reportInsights = allInsights
            .Select(insight => new EngineeringReportInsight
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                MetricType = insight.MetricType,
                Category = insight.Category,
                Title = insight.Title,
                Description = insight.Description,
                Impact = insight.Impact,
                Recommendation = insight.Recommendation
            })
            .ToList();

        var reportActions = generatedActions
            .Select(action => new EngineeringAction
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                MetricType = action.MetricType,
                Title = action.Title,
                Description = action.Description,
                Priority = action.Priority,
                Owner = null,
                DueDate = null,
                Status = ActionStatus.Todo,
                CreatedAt = DateTimeOffset.UtcNow
            })
            .ToList();

        report.SnapshotJson = new EngineeringReportSnapshot(
            1,
            sourceMetrics.Select(metric =>
            {
                var definition = MetricDefinitions.Find(metric.MetricType);
                return new AIAnalysisMetricContext(metric.MetricType, metric.Value,
                    metric.DataStatus, definition?.Unit, definition?.TemporalSemantics,
                    definition?.Description);
            }).ToList(),
            metrics.Select(metric => new EngineeringReportMetricResponse(
                metric.Key, metric.Value, metricScoreCalculator.Calculate(metric.Key, metric.Value))).ToList(),
            trends).Serialize();

        await reportRepository.AddAsync(
            report,
            cancellationToken);

        if (reportInsights.Count > 0)
        {
            await insightRepository.AddRangeAsync(
                reportInsights,
                cancellationToken);
        }
        
        await actionRepository.AddRangeAsync(
            reportActions,
            cancellationToken);
        
        if (generatedRisks.Count > 0)
        {
            await riskRepository.AddRangeAsync(
                generatedRisks,
                cancellationToken);
        }

        await reportRepository.SaveChangesAsync(
            cancellationToken);

        return ToResponse(
            report,
            healthScore.HealthLevel,
            metrics,
            reportInsights
                .Select(ToInsightResponse)
                .ToList(),
            reportActions
                .Select(ToActionResponse)
                .ToList(),
            generatedRisks
                .Select(ToRiskResponse)
                .ToList(),
            trends);
    }

    public async Task<EngineeringReportResponse?> GetByIdAsync(
        Guid teamId,
        Guid reportId,
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

        var report = await reportRepository.GetByIdAsync(
            reportId,
            teamId,
            cancellationToken);

        if (report is null)
        {
            return null;
        }

        var insights = await insightRepository.GetByReportIdAsync(
            report.Id,
            cancellationToken);
        
        var actions = await actionRepository.GetByReportIdAsync(
            reportId,
            cancellationToken);

        var risks = await riskRepository.GetByReportIdAsync(
            reportId,
            cancellationToken);
        
        var snapshot = EngineeringReportSnapshot.Read(report);
        var metrics = snapshot?.Metrics.ToDictionary(x => x.MetricType, x => x.Value)
                ?? new Dictionary<MetricType, decimal>();
        IReadOnlyList<MetricTrendResult> trends = snapshot?.Trends ?? [];
        
        return ToResponse(
            report,
            EngineeringHealthLevelResolver.Resolve(
                report.OverallScore,
                report.DataCoverage),
            metrics,
            insights
                .Select(ToInsightResponse)
                .ToList(),
            actions
                .Select(ToActionResponse)
                .ToList(),
            risks
                .Select(ToRiskResponse)
                .ToList(),
            trends
            );
    }

    public async Task<IReadOnlyList<EngineeringReportResponse>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return [];
        }

        var reports = await reportRepository.GetByTeamAsync(
            teamId,
            cancellationToken);

        var responses = new List<EngineeringReportResponse>();

        foreach (var report in reports)
        {
            var insights = await insightRepository.GetByReportIdAsync(
                report.Id,
                cancellationToken);

            var actions = await actionRepository.GetByReportIdAsync(
                report.Id,
                cancellationToken);

            var risks = await riskRepository.GetByReportIdAsync(
                report.Id,
                cancellationToken);
            
            var snapshot = EngineeringReportSnapshot.Read(report);
            var metrics = snapshot?.Metrics.ToDictionary(x => x.MetricType, x => x.Value)
                ?? new Dictionary<MetricType, decimal>();
            IReadOnlyList<MetricTrendResult> trends = snapshot?.Trends ?? [];
            
            responses.Add(
                ToResponse(
                    report,
                    EngineeringHealthLevelResolver.Resolve(
                        report.OverallScore,
                        report.DataCoverage),
                    metrics,
                    insights
                        .Select(ToInsightResponse)
                        .ToList(),
                    actions
                        .Select(ToActionResponse)
                        .ToList(),
                    risks
                        .Select(ToRiskResponse)
                        .ToList(),
                    trends));
        }

        return responses;
    }

    private EngineeringReportResponse ToResponse(
        EngineeringReport report,
        string healthLevel,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        IReadOnlyList<EngineeringReportInsightResponse> insights,
        IReadOnlyList<EngineeringActionResponse> actions,
        IReadOnlyList<EngineeringReportRiskResponse> risks,
        IReadOnlyList<MetricTrendResult> trends)
    {
        var reportTrends = trends
            .Select(ToTrendResponse)
            .ToList();
        
        return new EngineeringReportResponse(
            report.Id,
            report.TeamId,
            report.PeriodStart,
            report.PeriodEnd,
            report.ExecutiveSummary,
            report.OverallScore,
            healthLevel,
            report.DataCoverage,
            report.CreatedAt,
            EngineeringReportSnapshot.Read(report)?.Metrics ?? [],
            insights,
            actions,
            risks,
            reportTrends,
            report.SnapshotJson is not null);
    }

    private static string BuildExecutiveSummary(
        int overallScore,
        string healthLevel)
    {
        return
            $"Engineering team health is {healthLevel} " +
            $"with an overall score of {overallScore}/100 " +
            "for the selected period.";
    }
    
    private async Task<Dictionary<MetricType, decimal>> GetMetricsAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var metrics = new Dictionary<MetricType, decimal>();

        await AddMetricAsync(
            metrics,
            teamId,
            MetricType.CycleTime,
            periodStart,
            periodEnd,
            cancellationToken);

        await AddMetricAsync(
            metrics,
            teamId,
            MetricType.PRReviewTime,
            periodStart,
            periodEnd,
            cancellationToken);

        await AddMetricAsync(
            metrics,
            teamId,
            MetricType.DeploymentFrequency,
            periodStart,
            periodEnd,
            cancellationToken);

        await AddMetricAsync(
            metrics,
            teamId,
            MetricType.ChangeFailureRate,
            periodStart,
            periodEnd,
            cancellationToken);

        await AddMetricAsync(
            metrics,
            teamId,
            MetricType.LeadTime,
            periodStart,
            periodEnd,
            cancellationToken);

        await AddMetricAsync(
            metrics,
            teamId,
            MetricType.OpenPRs,
            periodStart,
            periodEnd,
            cancellationToken);

        await AddMetricAsync(
            metrics,
            teamId,
            MetricType.MergedPRs,
            periodStart,
            periodEnd,
            cancellationToken);

        await AddMetricAsync(
            metrics,
            teamId,
            MetricType.BlockedItems,
            periodStart,
            periodEnd,
            cancellationToken);

        return metrics;
    }
    
    private async Task AddMetricAsync(
        Dictionary<MetricType, decimal> metrics,
        Guid teamId,
        MetricType metricType,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var metric = await metricRepository.GetByTeamAndPeriodAsync(
            teamId,
            metricType,
            periodStart,
            periodEnd,
            cancellationToken);

        if (metric is null)
        {
            return;
        }

        if (metric.DataStatus != MetricDataStatus.Available)
        {
            return;
        }

        if (!metric.Value.HasValue)
        {
            return;
        }

        metrics[metricType] = metric.Value.Value;
    }
    
    private static EngineeringReportInsightResponse ToInsightResponse(
        EngineeringReportInsight insight)
    {
        return new EngineeringReportInsightResponse(
            insight.Id,
            insight.ReportId,
            insight.MetricType.ToString() ?? string.Empty,
            insight.Category.ToString(),
            insight.Title,
            insight.Description,
            insight.Impact,
            insight.Recommendation);
    }
    
    private static EngineeringActionResponse ToActionResponse(
        EngineeringAction action)
    {
        return new EngineeringActionResponse(
            action.Id,
            action.ReportId,
            action.MetricType?.ToString(),
            action.Title,
            action.Description,
            action.Priority,
            action.Owner,
            action.DueDate,
            action.Status.ToString(),
            action.CreatedAt);
    }
    
    private static EngineeringReportRiskResponse ToRiskResponse(
        EngineeringRisk risk)
    {
        return new EngineeringReportRiskResponse(
            risk.Id,
            risk.ReportId,
            risk.Severity,
            risk.Category,
            risk.Title,
            risk.Description,
            risk.Recommendation,
            risk.CreatedAt);
    }
    
    private static EngineeringMetricTrendResponse ToTrendResponse(
        MetricTrendResult trend)
    {
        return new EngineeringMetricTrendResponse(
            trend.MetricType.ToString(),
            trend.CurrentValue,
            trend.PreviousValue,
            trend.ChangePercentage,
            trend.Direction.ToString());
    }
    
    private async Task<IReadOnlyList<MetricTrendResult>> GetTrendsAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyDictionary<MetricType, decimal> currentMetrics,
        CancellationToken cancellationToken)
    {
        var previousPeriod = previousPeriodCalculator.Calculate(
            periodStart,
            periodEnd);

        var previousMetrics = await GetMetricsAsync(
            teamId,
            previousPeriod.Start,
            previousPeriod.End,
            cancellationToken);

        return metricTrendBuilder.Build(
            currentMetrics,
            previousMetrics);
    }
}