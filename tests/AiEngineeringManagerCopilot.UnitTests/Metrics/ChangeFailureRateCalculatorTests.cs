using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class ChangeFailureRateCalculatorTests
{
    private readonly ChangeFailureRateCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldReturnFailureRate()
    {
        var deployments = new[]
        {
            CreateDeployment("success", "2026-01-10"),
            CreateDeployment("success", "2026-01-11"),
            CreateDeployment("failure", "2026-01-12"),
            CreateDeployment("failure", "2026-01-13")
        };

        var result = _calculator.Calculate(
            deployments,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.TotalDeployments.Should().Be(4);
        result.FailedDeployments.Should().Be(2);
        result.FailureRate.Should().Be(50m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoDeploymentsExist()
    {
        var result = _calculator.Calculate(
            Array.Empty<Deployment>(),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.TotalDeployments.Should().Be(0);
        result.FailedDeployments.Should().Be(0);
        result.FailureRate.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldIgnoreDeploymentsOutsidePeriod()
    {
        var deployments = new[]
        {
            CreateDeployment("failure", "2025-12-31"),
            CreateDeployment("success", "2026-01-10"),
            CreateDeployment("failure", "2026-02-01")
        };

        var result = _calculator.Calculate(
            deployments,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.TotalDeployments.Should().Be(1);
        result.FailedDeployments.Should().Be(0);
        result.FailureRate.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldTreatCancelledAsNonFailure()
    {
        var deployments = new[]
        {
            CreateDeployment("success", "2026-01-10"),
            CreateDeployment("cancelled", "2026-01-11"),
            CreateDeployment("failure", "2026-01-12")
        };

        var result = _calculator.Calculate(
            deployments,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.TotalDeployments.Should().Be(3);
        result.FailedDeployments.Should().Be(1);
        result.FailureRate.Should().Be(33.33m);
    }

    [Fact]
    public void Calculate_ShouldRejectInvalidPeriod()
    {
        var action = () => _calculator.Calculate(
            Array.Empty<Deployment>(),
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 1));

        action.Should()
            .Throw<ArgumentException>();
    }

    private static Deployment CreateDeployment(
        string status,
        string date)
    {
        return new Deployment
        {
            Id = Guid.NewGuid(),
            RepositoryId = Guid.NewGuid(),
            ExternalId = Random.Shared.NextInt64(
                1,
                long.MaxValue),
            Environment = "production",
            Status = status,
            DeployedAt = DateTimeOffset.Parse(
                $"{date}T12:00:00+00:00")
        };
    }
}