using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IEngineeringActionGenerator
{
    IReadOnlyList<EngineeringActionSuggestion> Generate(
        IReadOnlyList<EngineeringInsight> insights,
        IReadOnlyList<EngineeringRisk> risks);
}