using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class DeploymentFrequencyCalculator
    : IDeploymentFrequencyCalculator
{
    public DeploymentFrequencyResult Calculate(
        IReadOnlyList<Deployment> deployments,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        if (periodStart > periodEnd)
        {
            throw new ArgumentException(
                "periodStart must be before or equal to periodEnd.");
        }

        var start = new DateTimeOffset(
            periodStart.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var end = new DateTimeOffset(
            periodEnd
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var count = deployments.Count(x =>
            x.Status.Equals(
                "success",
                StringComparison.OrdinalIgnoreCase) &&
            x.DeployedAt >= start &&
            x.DeployedAt < end);

        return new DeploymentFrequencyResult(count);
    }
}