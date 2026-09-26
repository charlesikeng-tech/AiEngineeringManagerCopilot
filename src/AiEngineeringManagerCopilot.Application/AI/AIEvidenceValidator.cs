using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.AI;

public sealed class AIEvidenceValidator
{
    public void Validate(
        IReadOnlyList<LlmEvidenceResult> evidence,
        IReadOnlyDictionary<MetricType, decimal> metrics)
    {
        foreach (var item in evidence)
        {
            if (!Enum.TryParse<MetricType>(
                    item.MetricType,
                    ignoreCase: true,
                    out var metricType))
            {
                throw new InvalidOperationException(
                    $"AI evidence references unknown metric '{item.MetricType}'.");
            }

            if (!metrics.TryGetValue(metricType, out var actualValue))
            {
                throw new InvalidOperationException(
                    $"AI evidence references unavailable metric '{item.MetricType}'.");
            }

            if (item.Value != actualValue)
            {
                throw new InvalidOperationException(
                    $"AI evidence value for '{item.MetricType}' " +
                    $"does not match the actual metric value.");
            }

            if (item.Confidence is < 0 or > 1)
            {
                throw new InvalidOperationException(
                    "AI evidence confidence must be between 0 and 1.");
            }
        }
    }
}