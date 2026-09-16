using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public interface IEngineeringInsightGenerator
{
    IReadOnlyList<EngineeringInsight> Generate(
        IReadOnlyDictionary<MetricType, decimal> metrics);
}