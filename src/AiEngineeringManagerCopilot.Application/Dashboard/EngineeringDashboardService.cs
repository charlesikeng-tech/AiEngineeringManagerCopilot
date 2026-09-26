using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Dashboard;

public sealed class EngineeringDashboardService(
    IEngineeringMetricRepository metricRepository,
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IEngineeringHealthScoreCalculator healthScoreCalculator,
    PreviousPeriodCalculator previousPeriodCalculator,
    MetricTrendBuilder metricTrendBuilder,
    IEngineeringReportRepository reportRepository,
    IEngineeringRiskRepository riskRepository,
    IEngineeringReportService reportService,
    IAIAnalysisService aiAnalysisService)
    : IEngineeringDashboardService
{
    public async Task<EngineeringDashboardResponse?> GetAsync(
        Guid teamId,
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

        var metrics = await metricRepository.GetLatestByTeamAsync(
            teamId,
            cancellationToken);

        var metricsByType = metrics.ToDictionary(
            x => x.MetricType,
            x => x.Value);

        decimal? GetMetric(MetricType metricType)
        {
            return metricsByType.TryGetValue(
                metricType,
                out var value)
                ? value
                : null;
        }

        int? GetIntMetric(MetricType metricType)
        {
            return metricsByType.TryGetValue(
                metricType,
                out var value)
                ? (int)value
                : null;
        }

        var healthScore = healthScoreCalculator.Calculate(
            GetMetric(MetricType.CycleTime),
            GetMetric(MetricType.PRReviewTime),
            GetIntMetric(MetricType.DeploymentFrequency),
            GetMetric(MetricType.ChangeFailureRate),
            GetMetric(MetricType.LeadTime),
            GetIntMetric(MetricType.OpenPRs),
            GetIntMetric(MetricType.MergedPRs),
            GetIntMetric(MetricType.BlockedItems));
        
        IReadOnlyList<MetricTrendResult> trends = [];

        if (metrics.Count > 0)
        {
            var currentPeriodStart = metrics.Max(x => x.PeriodStart);
            var currentPeriodEnd = metrics.Max(x => x.PeriodEnd);

            var previousPeriod = previousPeriodCalculator.Calculate(
                currentPeriodStart,
                currentPeriodEnd);

            var previousMetricEntities =
                await metricRepository.GetByTeamAndPeriodAsync(
                    teamId,
                    previousPeriod.Start,
                    previousPeriod.End,
                    cancellationToken);

            var previousMetrics = previousMetricEntities
                .GroupBy(x => x.MetricType)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(x => x.CreatedAt)
                        .First()
                        .Value);

            trends = metricTrendBuilder.Build(
                metricsByType,
                previousMetrics);
        }

        var metricResponses = metrics
            .Select(x => new EngineeringMetricResponse(
                x.Id,
                x.TeamId,
                x.MetricType,
                x.Value,
                x.PeriodStart,
                x.PeriodEnd,
                x.CreatedAt))
            .ToList();
        
        var reports = await reportRepository.GetByTeamAsync(
            teamId,
            cancellationToken);

        var latestReport = reports
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        IReadOnlyList<EngineeringDashboardRiskResponse> risks = [];
        
        EngineeringReportResponse? latestReportResponse = null;
        AIAnalysisResult? aiAnalysis = null;

        if (latestReport is not null)
        {
            var reportRisks = await riskRepository.GetByReportIdAsync(
                latestReport.Id,
                cancellationToken);

            risks = reportRisks
                .Select(x => new EngineeringDashboardRiskResponse(
                    x.Id,
                    x.ReportId,
                    x.MetricType,
                    x.Severity,
                    x.Category,
                    x.Title,
                    x.Description,
                    x.Recommendation,
                    x.CreatedAt))
                .ToList();
            
            latestReportResponse = await reportService.GetByIdAsync(
                teamId,
                latestReport.Id,
                cancellationToken);
            
            aiAnalysis = await aiAnalysisService.GetAsync(
                teamId,
                latestReport.Id,
                cancellationToken);
        }
        
        return new EngineeringDashboardResponse(
            teamId,
            metricResponses,
            healthScore,
            trends,
            risks,
            latestReportResponse,
            aiAnalysis);
    }
}