using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Risks;

public interface IEngineeringRiskDetector
{
    IReadOnlyList<EngineeringRisk> Detect(
        Guid teamId,
        Guid reportId,
        IReadOnlyDictionary<MetricType, decimal> metrics);
}