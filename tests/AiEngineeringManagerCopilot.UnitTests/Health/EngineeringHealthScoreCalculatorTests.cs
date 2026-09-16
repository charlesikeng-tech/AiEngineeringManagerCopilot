using AiEngineeringManagerCopilot.Application.Health;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Health;

public sealed class EngineeringHealthScoreCalculatorTests
{
    private readonly EngineeringHealthScoreCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldReturn100_WhenAllMetricsAreExcellent()
    {
        var result = _calculator.Calculate(
            cycleTimeHours: 8,
            prReviewTimeHours: 4,
            deploymentCount: 20,
            changeFailureRate: 5,
            leadTimeHours: 24,
            openPullRequests: 2,
            mergedPullRequests: 20,
            blockedItems: 0);

        result.OverallScore.Should().Be(100);
        result.HealthLevel.Should().Be("Excellent");
    }

    [Fact]
    public void Calculate_ShouldReturn78_ForExpectedMixedMetrics()
    {
        var result = _calculator.Calculate(
            cycleTimeHours: 24,
            prReviewTimeHours: 24,
            deploymentCount: 20,
            changeFailureRate: 10,
            leadTimeHours: 48,
            openPullRequests: 5,
            mergedPullRequests: 5,
            blockedItems: 0);

        result.OverallScore.Should().Be(81);
        result.HealthLevel.Should().Be("Healthy");
    }

    [Fact]
    public void Calculate_ShouldReturnCritical_WhenAllMetricsAreWorst()
    {
        var result = _calculator.Calculate(
            cycleTimeHours: 100,
            prReviewTimeHours: 100,
            deploymentCount: 0,
            changeFailureRate: 50,
            leadTimeHours: 500,
            openPullRequests: 50,
            mergedPullRequests: 0,
            blockedItems: 20);

        result.OverallScore.Should().Be(20);
        result.HealthLevel.Should().Be("Critical");
    }

    [Fact]
    public void Calculate_ShouldReturnNeedsAttention_WhenScoreIsBetween60And74()
    {
        var result = _calculator.Calculate(
            cycleTimeHours: 48,
            prReviewTimeHours: 24,
            deploymentCount: 5,
            changeFailureRate: 20,
            leadTimeHours: 72,
            openPullRequests: 10,
            mergedPullRequests: 5,
            blockedItems: 5);

        result.OverallScore.Should().Be(60);
        result.HealthLevel.Should().Be("Needs Attention");
    }

    [Fact]
    public void Calculate_ShouldReturnAtRisk_WhenScoreIsBetween40And59()
    {
        var result = _calculator.Calculate(
            cycleTimeHours: 72,
            prReviewTimeHours: 48,
            deploymentCount: 1,
            changeFailureRate: 30,
            leadTimeHours: 168,
            openPullRequests: 20,
            mergedPullRequests: 1,
            blockedItems: 10);

        result.OverallScore.Should().Be(40);
        result.HealthLevel.Should().Be("At Risk");
    }
}