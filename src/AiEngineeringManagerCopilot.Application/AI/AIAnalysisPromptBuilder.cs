using System.Globalization;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.AI;

public sealed class AIAnalysisPromptBuilder
{
    public string Build(AIAnalysisContext context)
    {
        var availableMetrics = string.Join(
            Environment.NewLine,
            context.Metrics
                .Where(x =>
                    x.DataStatus == MetricDataStatus.Available &&
                    x.Value.HasValue)
                .Select(x =>
                    $"- {x.MetricType}: " +
                    $"{x.Value!.Value.ToString("0.##", CultureInfo.InvariantCulture)} " +
                    $"{x.Unit} | " +
                    $"temporal semantics: {x.TemporalSemantics} | " +
                    $"{x.Description}"));
        var unavailableMetrics = string.Join(
            Environment.NewLine,
            context.Metrics
                .Where(x => x.DataStatus != MetricDataStatus.Available)
                .Select(x =>
                    $"- {x.MetricType}: {x.DataStatus}"));

        var insights = string.Join(
            Environment.NewLine,
            context.Insights.Select(x =>
                $"- {x.Category}: {x.Title} — {x.Description}"));

        var risks = string.Join(
            Environment.NewLine,
            context.Risks.Select(x =>
                $"- {x.Severity} / {x.Category}: {x.Title} — {x.Description}"));

        var trends = string.Join(
            Environment.NewLine,
            context.Trends.Select(x =>
            {
                var previousValue = x.PreviousValue.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture);

                var currentValue = x.CurrentValue.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture);

                var change = x.ChangePercentage.HasValue
                    ? x.ChangePercentage.Value.ToString(
                        "+0.##;-0.##;0",
                        CultureInfo.InvariantCulture) + "%"
                    : "N/A";

                return
                    $"- {x.MetricType}: " +
                    $"{previousValue} → {currentValue} " +
                    $"({change}) — {x.Direction}";
            }));

        var periodStart = context.PeriodStart.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        var periodEnd = context.PeriodEnd.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        var dataCoverage = context.DataCoverage.ToString(
            "0.##",
            CultureInfo.InvariantCulture);

        return $$"""
            You are an Engineering Manager Copilot.

            Analyze the engineering health of the team using only the
            engineering evidence provided below.

            ## Period

            {{periodStart}} to {{periodEnd}}

            ## Engineering health

            Overall score: {{context.OverallScore}}/100
            Data coverage: {{dataCoverage}}%

            ## Executive summary

            {{context.ExecutiveSummary}}

            ## Available metrics

            {{availableMetrics}}

            ## Unavailable metrics

            {{unavailableMetrics}}

            ## Metric trends compared with previous period

            {{trends}}

            ## Existing insights

            {{insights}}

            ## Detected risks

            {{risks}}

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

            ## Data interpretation rules

            The data coverage percentage represents how much of the expected
            engineering health signal is currently observable.

            Take data coverage into account when determining the strength
            and confidence of conclusions.

            Always mention limited data coverage in the executive assessment
            when coverage is below 70%.

            When coverage is below 50%, explicitly state that the analysis is
            partial and that conclusions apply only to the observable
            engineering signals.

            SourceNotConfigured means that this engineering dimension is not
            observable by the system. It does not indicate poor performance.

            NoData means that the source is configured but no usable observation
            was available for the selected period. It does not indicate a zero
            value or poor performance.

            A metric marked Available with value 0 is an observed zero value.
            Do not reinterpret it as missing data.
            
            Metric definitions, units, and temporal semantics provided with a metric
            are authoritative.
            
            A CurrentSnapshot metric describes the state observed when the metric
            was calculated. It must not be described as having occurred during the
            reporting period unless additional evidence explicitly supports that
            conclusion.
            
            A ReportingPeriod metric describes observations associated with the
            selected reporting period.
            
            Do not infer historical state or duration from a CurrentSnapshot metric.
            
            Do not invent or reinterpret metric units when a unit is provided.

            Do not infer team-wide engineering performance from unavailable
            metrics.

            Do not present an observed metric as proof of the complete
            real-world state when the metric may depend on integration or
            telemetry coverage.

            For zero-valued operational metrics such as DeploymentFrequency,
            distinguish between "zero observed/recorded by the system" and
            "zero actually occurred" unless the provided data proves complete
            observability.

            Do not invent organizational policies, SLAs, SLOs, sprint cadences,
            release cadences, or performance targets.

            When recommending a target or cadence that is not present in the
            data, present it as something the Engineering Manager should define
            based on team context, not as a prescribed target.

            ## Evidence

            Every important conclusion should be supported by observable
            engineering evidence from the available metrics and metric trends
            provided above.

            Evidence proves only what the provided metric directly measures.
            Do not extend evidence beyond the observable scope of the metric.

            For each evidence item:
            - metricType must reference an Available metric;
            - value must exactly match the current value of that metric;
            - reason must explain why the metric supports the analysis;
            - confidence must be between 0 and 1;
            - confidence represents how strongly the available engineering data
              supports the conclusion.

            Never use NoData or SourceNotConfigured metrics as evidence.

            Do not invent metrics, metric values, targets, units, benchmarks,
            incidents, causes, or trends.

            Prefer measurable engineering signals over assumptions.

            Use higher confidence when evidence is direct and supported by
            multiple consistent signals.

            Use lower confidence when the interpretation is uncertain or when
            data coverage is limited.

            If the available data does not support a conclusion, do not make it.

            Provide practical and actionable recommendations.
            """;
    }
    
    private static string FormatMetric(AIAnalysisMetricContext metric)
    {
        var value = metric.Value!.Value.ToString(
            "0.##",
            CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(metric.Unit) ||
            metric.TemporalSemantics is null ||
            string.IsNullOrWhiteSpace(metric.Description))
        {
            return $"- {metric.MetricType}: {value}";
        }

        return
            $"- {metric.MetricType}: {value} {metric.Unit} | " +
            $"temporal semantics: {metric.TemporalSemantics} | " +
            metric.Description;
    }
}