using System.Globalization;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.AI;

public sealed class AIAnalysisService(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
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
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            throw new KeyNotFoundException(
                $"Report '{reportId}' was not found.");
        }
        
        var report = await reportRepository.GetByIdAsync(
            reportId,
            teamId,
            cancellationToken);

        if (report is null)
        {
            throw new KeyNotFoundException(
                $"Report '{reportId}' was not found.");
        }

        // Idempotence:
        // if an analysis already exists for this report,
        // return the persisted result without calling the LLM again.
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

        // Metrics that can safely participate in calculations,
        // trends and evidence validation.
        var metrics = await GetAvailableMetricsAsync(
            teamId,
            report.PeriodStart,
            report.PeriodEnd,
            cancellationToken);

        // Complete metric context for the LLM.
        // This includes Available, NoData and SourceNotConfigured.
        var metricContext = await GetMetricContextAsync(
            teamId,
            report.PeriodStart,
            report.PeriodEnd,
            cancellationToken);

        var previousPeriod = previousPeriodCalculator.Calculate(
            report.PeriodStart,
            report.PeriodEnd);

        var previousMetrics = await GetAvailableMetricsAsync(
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
            report.DataCoverage,
            report.ExecutiveSummary,
            metricContext,
            analysisInsights,
            risks,
            trends);

        var prompt = promptBuilder.Build(context);

        var result = await llmProvider.AnalyzeAsync(
            prompt,
            cancellationToken);

        // The LLM sees the complete metric context,
        // but only Available metrics can be accepted as evidence.
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

    /// <summary>
    /// Returns only metrics that contain an actual observable value.
    ///
    /// These metrics are used for deterministic calculations,
    /// trends and evidence validation.
    /// </summary>
    private async Task<IReadOnlyDictionary<MetricType, decimal>>
        GetAvailableMetricsAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        var metrics = new Dictionary<MetricType, decimal>();

        foreach (var metricType in Enum.GetValues<MetricType>())
        {
            var metric = await metricRepository.GetByTeamAndPeriodAsync(
                teamId,
                metricType,
                periodStart,
                periodEnd,
                cancellationToken);

            if (metric is null)
            {
                continue;
            }

            if (metric.DataStatus != MetricDataStatus.Available)
            {
                continue;
            }

            if (!metric.Value.HasValue)
            {
                continue;
            }

            metrics[metricType] = metric.Value.Value;
        }

        return metrics;
    }

    /// <summary>
    /// Returns the complete metric state for the AI context.
    ///
    /// Unlike GetAvailableMetricsAsync, this method intentionally keeps
    /// NoData and SourceNotConfigured metrics so that the LLM understands
    /// which engineering dimensions are not observable.
    /// </summary>
    private async Task<IReadOnlyList<AIAnalysisMetricContext>>
        GetMetricContextAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        var metrics = new List<AIAnalysisMetricContext>();

        foreach (var metricType in Enum.GetValues<MetricType>())
        {
            var metric = await metricRepository.GetByTeamAndPeriodAsync(
                teamId,
                metricType,
                periodStart,
                periodEnd,
                cancellationToken);

            if (metric is null)
            {
                continue;
            }

            var definition = MetricDefinitions.Find(metric.MetricType);

            metrics.Add(
                new AIAnalysisMetricContext(
                    metric.MetricType,
                    metric.Value,
                    metric.DataStatus,
                    definition?.Unit,
                    definition?.TemporalSemantics,
                    definition?.Description));
        }

        return metrics;
    }
}