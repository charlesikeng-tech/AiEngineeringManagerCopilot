using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed class EngineeringActionGenerator
    : IEngineeringActionGenerator
{
    public IReadOnlyList<EngineeringActionSuggestion> Generate(
        IReadOnlyList<EngineeringInsight> insights, 
        IReadOnlyList<EngineeringRisk> risks)
    {
        var actions = insights
            .Select(insight => CreateAction(insight, risks))
            .OrderByDescending(x => x.Priority)
            .Take(3)
            .ToList();

        return actions;
    }

    private static EngineeringActionSuggestion CreateAction(
        EngineeringInsight insight,
        IReadOnlyList<EngineeringRisk> risks)
    {
        var basePriority =
            EngineeringActionPolicy.GetPriority(insight.Category);

        var matchingRisk = risks
            .Where(x => x.MetricType == insight.MetricType)
            .OrderByDescending(x => x.Severity)
            .FirstOrDefault();

        var priority = matchingRisk is null
            ? basePriority
            : Max(
                basePriority,
                ToActionPriority(matchingRisk.Severity));

        return new EngineeringActionSuggestion(
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