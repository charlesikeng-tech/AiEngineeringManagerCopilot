using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Actions;

public sealed class EngineeringActionGenerator
    : IEngineeringActionGenerator
{
    private const int MaxActions = 3;

    public IReadOnlyList<EngineeringActionSuggestion> Generate(
        IReadOnlyList<EngineeringInsight> insights,
        IReadOnlyList<EngineeringRisk> risks)
    {
        ArgumentNullException.ThrowIfNull(insights);
        ArgumentNullException.ThrowIfNull(risks);

        return insights
            .Select((insight, index) => new
            {
                Action = CreateAction(insight, risks),
                OriginalIndex = index
            })
            .OrderByDescending(x => x.Action.Priority)
            .ThenBy(x => x.OriginalIndex)
            .Take(MaxActions)
            .Select(x => x.Action)
            .ToList();
    }

    private static EngineeringActionSuggestion CreateAction(
        EngineeringInsight insight,
        IReadOnlyList<EngineeringRisk> risks)
    {
        var basePriority =
            EngineeringActionPolicy.GetPriority(insight.Category);

        var matchingRisk = risks
            .Where(risk => risk.MetricType == insight.MetricType)
            .OrderByDescending(risk => risk.Severity)
            .FirstOrDefault();

        var priority = matchingRisk is null
            ? basePriority
            : Max(
                basePriority,
                ToActionPriority(matchingRisk.Severity));

        return new EngineeringActionSuggestion(
            insight.MetricType,
            insight.Title,
            insight.Recommendation,
            priority);
    }

    private static ActionPriority ToActionPriority(
        RiskSeverity severity)
    {
        return severity switch
        {
            RiskSeverity.Critical => ActionPriority.Critical,
            RiskSeverity.High => ActionPriority.High,
            RiskSeverity.Medium => ActionPriority.Medium,
            _ => ActionPriority.Low
        };
    }

    private static ActionPriority Max(
        ActionPriority first,
        ActionPriority second)
    {
        return (ActionPriority)Math.Max(
            (int)first,
            (int)second);
    }
}