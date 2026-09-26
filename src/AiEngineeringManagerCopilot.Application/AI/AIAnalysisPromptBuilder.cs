using System.Globalization;

namespace AiEngineeringManagerCopilot.Application.AI;

public sealed class AIAnalysisPromptBuilder
{
    public string Build(AIAnalysisContext context)
    {
        var metrics = string.Join(
            Environment.NewLine,
            context.Metrics.Select(x =>
                $"- {x.Key}: {x.Value.ToString("0.##", CultureInfo.InvariantCulture)}"));

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

        return $$"""
            You are an Engineering Manager Copilot.

            Analyze the engineering health of the team.

            ## Period

            {{periodStart}} to {{periodEnd}}

            ## Overall score

            {{context.OverallScore}}/100

            ## Executive summary

            {{context.ExecutiveSummary}}

            ## Metrics

            {{metrics}}

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

            ## Evidence

            Every important conclusion should be supported by observable
            engineering evidence from the metrics and metric trends provided
            above.

            For each evidence item:
            - metricType must reference a metric present in the provided metrics;
            - value must be the current value of that metric;
            - reason must explain why the metric supports the analysis;
            - confidence must be between 0 and 1;
            - confidence represents how strongly the available engineering data
              supports the conclusion.

            Do not invent metrics or metric values.

            Prefer evidence based on measurable engineering signals rather than
            assumptions.

            Use higher confidence when the evidence is direct and supported by
            clear metric or trend data.

            Use lower confidence when the interpretation is less certain.

            If the available data does not support an evidence item, do not
            create it.

            Provide practical and actionable recommendations.
            """;
    }
}