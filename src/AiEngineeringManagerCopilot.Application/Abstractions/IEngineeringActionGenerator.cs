using AiEngineeringManagerCopilot.Application.Reports;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IEngineeringActionGenerator
{
    IReadOnlyList<EngineeringActionSuggestion> Generate(
        IReadOnlyList<EngineeringInsight> insights);
}