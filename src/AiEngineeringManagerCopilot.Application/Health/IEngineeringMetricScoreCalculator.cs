using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Health;

public interface IEngineeringMetricScoreCalculator
{
    int Calculate(
        MetricType metricType,
        decimal value);
}