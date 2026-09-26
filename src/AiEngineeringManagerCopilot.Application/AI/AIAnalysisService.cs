using System.Globalization;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Metrics;
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
    IAIAnalysisEvidenceRepository analysisEvidenceRepository,
    ILlmProvider llmProvider,
    PreviousPeriodCalculator previousPeriodCalculator,
    MetricTrendBuilder metricTrendBuilder,
    AIEvidenceValidator evidenceValidator,
    AIAnalysisPromptBuilder promptBuilder)
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

            var existingEvidence =
                await analysisEvidenceRepository.GetByAnalysisIdAsync(
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
                    .ToList(),
                existingEvidence
                    .Select(x => new LlmEvidenceResult(
                        x.MetricType,
                        x.Value,
                        x.Reason,
                        x.Confidence))
                .ToList());
        }

        var metrics = await GetMetricsAsync(
            teamId,
            report.PeriodStart,
            report.PeriodEnd,
            cancellationToken);
        
        var previousPeriod = previousPeriodCalculator.Calculate(
            report.PeriodStart,
            report.PeriodEnd);

        var previousMetrics = await GetMetricsAsync(
            teamId,
            previousPeriod.Start,
            previousPeriod.End,
            cancellationToken);

        var trends = metricTrendBuilder.Build(
            metrics,
            previousMetrics);

        var insights = await insightRepository.GetByReportIdAsync(
            reportId,
            cancellationToken);

        var risks = await riskRepository.GetByReportIdAsync(
            reportId,
            cancellationToken);

        var analysisInsights = insights
            .Select(x => new EngineeringInsight(
                x.MetricType,
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
            risks,
            trends);

        var prompt = promptBuilder.Build(context);

        var result = await llmProvider.AnalyzeAsync(
            prompt,
            cancellationToken);
        
        evidenceValidator.Validate(
            result.SafeEvidence,
            metrics);

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
        
        if (persistedActions.Count > 0)
        {
            await analysisActionRepository.AddRangeAsync(
                persistedActions,
                cancellationToken);
        }
        
        var persistedEvidence = result.SafeEvidence
            .Select(x =>
            {
                var evidence = new AIAnalysisEvidence
                {
                    Id = Guid.NewGuid(),
                    AIAnalysisId = analysis.Id,
                    MetricType = x.MetricType,
                    Value = x.Value,
                    Reason = x.Reason,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                evidence.SetConfidence(x.Confidence);

                return evidence;
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
        
        if (persistedEvidence.Count > 0)
        {
            await analysisEvidenceRepository.AddRangeAsync(
                persistedEvidence,
                cancellationToken);
        }

        await analysisRepository.SaveChangesAsync(
            cancellationToken);

        return new AIAnalysisResult(
            result.Summary,
            result.Insights,
            result.Actions,
            result.SafeEvidence);
    }

    public async Task<AIAnalysisResult?> GetAsync(
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
            return null;
        }

        var analysis = await analysisRepository.GetByReportIdAsync(
            reportId,
            cancellationToken);

        if (analysis is null)
        {
            return null;
        }

        var insights =
            await analysisInsightRepository.GetByAnalysisIdAsync(
                analysis.Id,
                cancellationToken);

        var actions =
            await analysisActionRepository.GetByAnalysisIdAsync(
                analysis.Id,
                cancellationToken);

        var evidence =
            await analysisEvidenceRepository.GetByAnalysisIdAsync(
                analysis.Id,
                cancellationToken);

        return new AIAnalysisResult(
            analysis.Summary,
            insights
                .Select(x => new LlmInsightResult(
                    x.Category,
                    x.Title,
                    x.Description,
                    x.Impact,
                    x.Recommendation))
                .ToList(),
            actions
                .Select(x => new LlmActionResult(
                    x.Title,
                    x.Description,
                    x.Priority))
                .ToList(),
            evidence
                .Select(x => new LlmEvidenceResult(
                    x.MetricType,
                    x.Value,
                    x.Reason,
                    x.Confidence))
                .ToList());
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
}