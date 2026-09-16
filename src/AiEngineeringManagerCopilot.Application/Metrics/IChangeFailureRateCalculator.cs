using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public interface IChangeFailureRateCalculator
{
    ChangeFailureRateResult Calculate(
        IReadOnlyList<Deployment> deployments,
        DateOnly periodStart,
        DateOnly periodEnd);
}