using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.AI;

public sealed class AIAnalysisService(
    IEngineeringReportRepository reportRepository,
    IEngineeringMetricRepository metricRepository,
    IEngineeringReportInsightRepository insightRepository,
    IEngineeringRiskRepository riskRepository,
    IAIAnalysisRepository analysisRepository,
    IAIAnalysisInsightRepository analysisInsightRepository,
    IAIAnalysisActionRepository analysisActionRepository,
    ILlmProvider llmProvider)
    : IAIAnalysisService
{
    public async Task<AIAnalysisResult> AnalyzeAsync(
        Guid teamId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(
            reportId,
            teamId,
            cancellationToken);

        if (report is null)
        {
            throw new KeyNotFoundException(
                $"Report '{reportId}' was not found.");
        }
        
        var existingAnalysis = await analysisRepository.GetByReportIdAsync(
            reportId,
            cancellationToken);

        if (existingAnalysis is not null)
        {
            var existingInsights =
                await analysisInsightRepository.GetByAnalysisIdAsync(
                    existingAnalysis.Id,
                    cancellationToken);

            var existingActions =
                await analysisActionRepository.GetByAnalysisIdAsync(
                    existingAnalysis.Id,
                    cancellationToken);

            return new AIAnalysisResult(
                existingAnalysis.Summary,
                existingInsights
                    .Select(x => new LlmInsightResult(
                        x.Category,
                        x.Title,
                        x.Description,
                        x.Impact,
                        x.Recommendation))
                    .ToList(),
                existingActions
                    .Select(x => new LlmActionResult(
                        x.Title,
                        x.Description,
                        x.Priority))
                    .ToList());
        }

        var metrics = await GetMetricsAsync(
            teamId,
            report.PeriodStart,
            report.PeriodEnd,
            cancellationToken);

        var insights = await insightRepository.GetByReportIdAsync(
            reportId,
            cancellationToken);

        var risks = await riskRepository.GetByReportIdAsync(
            reportId,
            cancellationToken);

        var analysisInsights = insights
            .Select(x => new EngineeringInsight(
                x.Category,
                x.Title,
                x.Description,
                x.Impact,
                x.Recommendation))
            .ToList();

        var context = new AIAnalysisContext(
            report.PeriodStart,
            report.PeriodEnd,
            report.OverallScore,
            report.ExecutiveSummary,
            metrics,
            analysisInsights,
            risks);

        var prompt = BuildPrompt(context);

        var result = await llmProvider.AnalyzeAsync(
            prompt,
            cancellationToken);

        var analysis = new AIAnalysis
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            Summary = result.Summary,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await analysisRepository.AddAsync(
            analysis,
            cancellationToken);

        var persistedInsights = result.Insights
            .Select(x => new AIAnalysisInsight
            {
                Id = Guid.NewGuid(),
                AIAnalysisId = analysis.Id,
                Category = x.Category,
                Title = x.Title,
                Description = x.Description,
                Impact = x.Impact,
                Recommendation = x.Recommendation
            })
            .ToList();

        var persistedActions = result.Actions
            .Select(x => new AIAnalysisAction
            {
                Id = Guid.NewGuid(),
                AIAnalysisId = analysis.Id,
                Title = x.Title,
                Description = x.Description,
                Priority = x.Priority
            })
            .ToList();

        if (persistedInsights.Count > 0)
        {
            await analysisInsightRepository.AddRangeAsync(
                persistedInsights,
                cancellationToken);
        }

        if (persistedActions.Count > 0)
        {
            await analysisActionRepository.AddRangeAsync(
                persistedActions,
                cancellationToken);
        }

        await analysisRepository.SaveChangesAsync(
            cancellationToken);

        return new AIAnalysisResult(
            result.Summary,
            result.Insights,
            result.Actions);
    }

    private async Task<IReadOnlyDictionary<MetricType, decimal>> GetMetricsAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var metrics = new Dictionary<MetricType, decimal>();

        foreach (var metricType in Enum.GetValues<MetricType>())
        {
            var values = await metricRepository.GetByTeamAndPeriodAsync(
                teamId,
                metricType,
                periodStart,
                periodEnd,
                cancellationToken);

            var metric = values.FirstOrDefault();

            if (metric is not null)
            {
                metrics[metricType] = metric.Value;
            }
        }

        return metrics;
    }

    private static string BuildPrompt(
        AIAnalysisContext context)
    {
        var metrics = string.Join(
            Environment.NewLine,
            context.Metrics.Select(x =>
                $"- {x.Key}: {x.Value}"));

        var insights = string.Join(
            Environment.NewLine,
            context.Insights.Select(x =>
                $"- {x.Category}: {x.Title} — {x.Description}"));

        var risks = string.Join(
            Environment.NewLine,
            context.Risks.Select(x =>
                $"- {x.Severity} / {x.Category}: {x.Title} — {x.Description}"));

        return $"""
            You are an Engineering Manager Copilot.

            Analyze the engineering health of the team.

            ## Period

            {context.PeriodStart} to {context.PeriodEnd}

            ## Overall score

            {context.OverallScore}/100

            ## Executive summary

            {context.ExecutiveSummary}

            ## Metrics

            {metrics}

            ## Existing insights

            {insights}

            ## Detected risks

            {risks}

            ## Objective

            Identify the most important engineering problems,
            their potential impact, and concrete actions an Engineering Manager
            should take.

            Focus on:
            - delivery efficiency
            - code review performance
            - deployment practices
            - quality and reliability
            - bottlenecks
            - engineering process
            - technical risks

            Provide practical and actionable recommendations.
            """;
    }
}