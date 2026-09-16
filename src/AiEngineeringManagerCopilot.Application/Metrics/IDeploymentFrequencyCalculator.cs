using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public interface IDeploymentFrequencyCalculator
{
    DeploymentFrequencyResult Calculate(
        IReadOnlyList<Deployment> deployments,
        DateOnly periodStart,
        DateOnly periodEnd);
}