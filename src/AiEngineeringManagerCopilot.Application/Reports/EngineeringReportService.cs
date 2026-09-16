using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed class EngineeringReportService(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IEngineeringHealthScoreService healthScoreService,
    IEngineeringReportRepository reportRepository,
    IEngineeringMetricRepository metricRepository,
    IEngineeringInsightGenerator insightGenerator,
    IEngineeringReportInsightRepository insightRepository,
    IEngineeringActionRepository actionRepository,
    IEngineeringActionGenerator actionGenerator,
    IEngineeringMetricScoreCalculator metricScoreCalculator,
    IEngineeringRiskDetector riskDetector,
    IEngineeringRiskRepository riskRepository)
    : IEngineeringReportService
{
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

        var healthScore = await healthScoreService.CalculateAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        if (healthScore is null)
        {
            return null;
        }
        
        var metrics = await GetMetricsAsync(
            teamId,
            periodStart,
            periodEnd,
            cancellationToken);

        var generatedInsights = insightGenerator.Generate(metrics);
        
        var generatedActions = actionGenerator.Generate(
            generatedInsights);
        
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
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        var generatedRisks = riskDetector.Detect(
            teamId,
            report.Id,
            metrics);
        
        var reportInsights = generatedInsights
            .Select(insight => new EngineeringReportInsight
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
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
                Title = action.Title,
                Description = action.Description,
                Priority = action.Priority,
                Owner = null,
                DueDate = null,
                Status = ActionStatus.Todo,
                CreatedAt = DateTimeOffset.UtcNow
            })
            .ToList();
        
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
                .ToList());
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
        
        var metrics = await GetMetricsAsync(
            teamId,
            report.PeriodStart,
            report.PeriodEnd,
            cancellationToken);
        
        return ToResponse(
            report,
            GetHealthLevel(report.OverallScore),
            metrics,
            insights
                .Select(ToInsightResponse)
                .ToList(),
            actions
                .Select(ToActionResponse)
                .ToList(),
            risks
                .Select(ToRiskResponse)
                .ToList()
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
            
            var metrics = await GetMetricsAsync(
                teamId,
                report.PeriodStart,
                report.PeriodEnd,
                cancellationToken);
            
            responses.Add(
                ToResponse(
                    report,
                    GetHealthLevel(report.OverallScore),
                    metrics,
                    insights
                        .Select(ToInsightResponse)
                        .ToList(),
                    actions
                        .Select(ToActionResponse)
                        .ToList(),
                    risks
                        .Select(ToRiskResponse)
                        .ToList()));
        }

        return responses;
    }

    private EngineeringReportResponse ToResponse(
        EngineeringReport report,
        string healthLevel,
        IReadOnlyDictionary<MetricType, decimal> metrics,
        IReadOnlyList<EngineeringReportInsightResponse> insights,
        IReadOnlyList<EngineeringActionResponse> actions,
        IReadOnlyList<EngineeringReportRiskResponse> risks)
    {
        var reportMetrics = metrics
            .OrderBy(x => x.Key)
            .Select(metric => new EngineeringReportMetricResponse(
                metric.Key,
                metric.Value,
                metricScoreCalculator.Calculate(
                    metric.Key,
                    metric.Value)))
            .ToList();

        return new EngineeringReportResponse(
            report.Id,
            report.TeamId,
            report.PeriodStart,
            report.PeriodEnd,
            report.ExecutiveSummary,
            report.OverallScore,
            healthLevel,
            report.CreatedAt,
            reportMetrics,
            insights,
            actions,
            risks);
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

    private static string GetHealthLevel(int score)
    {
        if (score >= 90)
        {
            return "Excellent";
        }

        if (score >= 75)
        {
            return "Healthy";
        }

        if (score >= 60)
        {
            return "Needs Attention";
        }

        if (score >= 40)
        {
            return "At Risk";
        }

        return "Critical";
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
        var values = await metricRepository.GetByTeamAndPeriodAsync(
            teamId,
            metricType,
            periodStart,
            periodEnd,
            cancellationToken);

        if (values.Count == 0)
        {
            return;
        }

        var latestValue = values
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Value)
            .First();

        metrics[metricType] = latestValue;
    }
    
    private static EngineeringReportInsightResponse ToInsightResponse(
        EngineeringReportInsight insight)
    {
        return new EngineeringReportInsightResponse(
            insight.Id,
            insight.ReportId,
            insight.Category,
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
}