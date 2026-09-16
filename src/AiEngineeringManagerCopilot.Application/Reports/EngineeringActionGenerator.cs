using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed class EngineeringActionGenerator
    : IEngineeringActionGenerator
{
    public IReadOnlyList<EngineeringActionSuggestion> Generate(
        IReadOnlyList<EngineeringInsight> insights)
    {
        var actions = insights
            .Select(CreateAction)
            .OrderByDescending(x => x.Priority)
            .Take(3)
            .ToList();

        return actions;
    }

    private static EngineeringActionSuggestion CreateAction(
        EngineeringInsight insight)
    {
        var priority = insight.Category switch
        {
            "Quality" => ActionPriority.Critical,
            "Reliability" => ActionPriority.Critical,
            "Delivery" => ActionPriority.High,
            "Review" => ActionPriority.High,
            "Process" => ActionPriority.Medium,
            _ => ActionPriority.Low
        };

        return new EngineeringActionSuggestion(
            insight.Title,
            insight.Recommendation,
            priority);
    }
}