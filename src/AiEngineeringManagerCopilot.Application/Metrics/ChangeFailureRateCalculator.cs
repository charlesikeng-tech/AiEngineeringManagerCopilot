using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class ChangeFailureRateCalculator
    : IChangeFailureRateCalculator
{
    public ChangeFailureRateResult Calculate(
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

        var deploymentsInPeriod = deployments
            .Where(x =>
                x.DeployedAt >= start &&
                x.DeployedAt < end)
            .ToList();

        var totalDeployments =
            deploymentsInPeriod.Count;

        var failedDeployments =
            deploymentsInPeriod.Count(x =>
                x.Status.Equals(
                    "failure",
                    StringComparison.OrdinalIgnoreCase));

        if (totalDeployments == 0)
        {
            return new ChangeFailureRateResult(
                0,
                0,
                0);
        }

        var failureRate =
            (decimal)failedDeployments /
            totalDeployments *
            100;

        return new ChangeFailureRateResult(
            Math.Round(
                failureRate,
                2,
                MidpointRounding.AwayFromZero),
            totalDeployments,
            failedDeployments);
    }
}