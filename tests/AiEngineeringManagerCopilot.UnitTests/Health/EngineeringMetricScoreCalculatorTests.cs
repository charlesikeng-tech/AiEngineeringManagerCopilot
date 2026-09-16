using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Health;

public sealed class EngineeringMetricScoreCalculatorTests
{
    private readonly EngineeringMetricScoreCalculator _calculator = new();

    [Theory]
    [InlineData(8, 100)]
    [InlineData(24, 80)]
    [InlineData(48, 60)]
    [InlineData(72, 40)]
    [InlineData(73, 20)]
    public void Calculate_CycleTime_ShouldReturnExpectedScore(
        decimal value,
        int expectedScore)
    {
        var result = _calculator.Calculate(
            MetricType.CycleTime,
            value);

        result.Should().Be(expectedScore);
    }

    [Theory]
    [InlineData(4, 100)]
    [InlineData(12, 80)]
    [InlineData(24, 60)]
    [InlineData(48, 40)]
    [InlineData(49, 20)]
    public void Calculate_PRReviewTime_ShouldReturnExpectedScore(
        decimal value,
        int expectedScore)
    {
        var result = _calculator.Calculate(
            MetricType.PRReviewTime,
            value);

        result.Should().Be(expectedScore);
    }

    [Theory]
    [InlineData(20, 100)]
    [InlineData(10, 80)]
    [InlineData(5, 60)]
    [InlineData(1, 40)]
    [InlineData(0, 20)]
    public void Calculate_DeploymentFrequency_ShouldReturnExpectedScore(
        decimal value,
        int expectedScore)
    {
        var result = _calculator.Calculate(
            MetricType.DeploymentFrequency,
            value);

        result.Should().Be(expectedScore);
    }

    [Theory]
    [InlineData(5, 100)]
    [InlineData(10, 80)]
    [InlineData(20, 60)]
    [InlineData(30, 40)]
    [InlineData(31, 20)]
    public void Calculate_ChangeFailureRate_ShouldReturnExpectedScore(
        decimal value,
        int expectedScore)
    {
        var result = _calculator.Calculate(
            MetricType.ChangeFailureRate,
            value);

        result.Should().Be(expectedScore);
    }

    [Theory]
    [InlineData(24, 100)]
    [InlineData(48, 80)]
    [InlineData(72, 60)]
    [InlineData(168, 40)]
    [InlineData(169, 20)]
    public void Calculate_LeadTime_ShouldReturnExpectedScore(
        decimal value,
        int expectedScore)
    {
        var result = _calculator.Calculate(
            MetricType.LeadTime,
            value);

        result.Should().Be(expectedScore);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(2, 100)]
    [InlineData(3, 80)]
    [InlineData(5, 80)]
    [InlineData(6, 60)]
    [InlineData(10, 60)]
    [InlineData(11, 40)]
    [InlineData(20, 40)]
    [InlineData(21, 20)]
    public void Calculate_OpenPRs_ShouldReturnExpectedScore(
        decimal value,
        int expectedScore)
    {
        var result = _calculator.Calculate(
            MetricType.OpenPRs,
            value);

        result.Should().Be(expectedScore);
    }

    [Theory]
    [InlineData(20, 100)]
    [InlineData(10, 80)]
    [InlineData(5, 60)]
    [InlineData(1, 40)]
    [InlineData(0, 20)]
    public void Calculate_MergedPRs_ShouldReturnExpectedScore(
        decimal value,
        int expectedScore)
    {
        var result = _calculator.Calculate(
            MetricType.MergedPRs,
            value);

        result.Should().Be(expectedScore);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 80)]
    [InlineData(2, 80)]
    [InlineData(3, 60)]
    [InlineData(5, 60)]
    [InlineData(6, 40)]
    [InlineData(10, 40)]
    [InlineData(11, 20)]
    public void Calculate_BlockedItems_ShouldReturnExpectedScore(
        decimal value,
        int expectedScore)
    {
        var result = _calculator.Calculate(
            MetricType.BlockedItems,
            value);

        result.Should().Be(expectedScore);
    }
}