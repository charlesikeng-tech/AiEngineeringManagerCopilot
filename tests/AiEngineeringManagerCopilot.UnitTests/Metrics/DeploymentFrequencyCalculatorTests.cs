using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class DeploymentFrequencyCalculatorTests
{
    private readonly DeploymentFrequencyCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldReturnDeploymentCount()
    {
        var deployments = new[]
        {
            CreateDeployment("success", "2026-01-10"),
            CreateDeployment("success", "2026-01-15"),
            CreateDeployment("success", "2026-01-20")
        };

        var result = _calculator.Calculate(
            deployments,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.DeploymentCount.Should().Be(3);
    }

    [Fact]
    public void Calculate_ShouldIgnoreFailedDeployments()
    {
        var deployments = new[]
        {
            CreateDeployment("success", "2026-01-10"),
            CreateDeployment("failure", "2026-01-15"),
            CreateDeployment("cancelled", "2026-01-20")
        };

        var result = _calculator.Calculate(
            deployments,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.DeploymentCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldIgnoreDeploymentsOutsidePeriod()
    {
        var deployments = new[]
        {
            CreateDeployment("success", "2025-12-31"),
            CreateDeployment("success", "2026-01-15"),
            CreateDeployment("success", "2026-02-01")
        };

        var result = _calculator.Calculate(
            deployments,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.DeploymentCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoDeploymentsExist()
    {
        var result = _calculator.Calculate(
            Array.Empty<Deployment>(),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.DeploymentCount.Should().Be(0);
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
            ExternalId = Random.Shared.NextInt64(1, long.MaxValue),
            Environment = "production",
            Status = status,
            DeployedAt = DateTimeOffset.Parse(
                $"{date}T12:00:00+00:00")
        };
    }
}