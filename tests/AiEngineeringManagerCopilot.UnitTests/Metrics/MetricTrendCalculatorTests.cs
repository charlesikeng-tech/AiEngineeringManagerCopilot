using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class MetricTrendCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnPositiveChange_WhenMetricIncreases()
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            MetricType.CycleTime,
            currentValue: 52m,
            previousValue: 31m);

        result.CurrentValue.Should().Be(52m);
        result.PreviousValue.Should().Be(31m);
        result.ChangePercentage.Should().Be(67.74m);
    }
    
    [Fact]
    public void Calculate_ShouldReturnNegativeChange_WhenMetricDecreases()
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            MetricType.CycleTime,
            currentValue: 30m,
            previousValue: 50m);

        result.ChangePercentage.Should().Be(-40m);
    }

    [Fact]
    public void Calculate_ShouldReturnZeroChange_WhenMetricIsStable()
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            MetricType.CycleTime,
            currentValue: 50m,
            previousValue: 50m);

        result.ChangePercentage.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldReturnNullChange_WhenPreviousValueIsZero()
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            MetricType.CycleTime,
            currentValue: 10m,
            previousValue: 0m);

        result.CurrentValue.Should().Be(10m);
        result.PreviousValue.Should().Be(0m);
        result.ChangePercentage.Should().BeNull();
    }
    
    [Fact]
    public void Calculate_ShouldReturnImproving_WhenCycleTimeDecreases()
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            MetricType.CycleTime,
            currentValue: 30m,
            previousValue: 50m);

        result.Direction.Should().Be(MetricTrendDirection.Improving);
    }
    
    [Fact]
    public void Calculate_ShouldReturnImproving_WhenDeploymentFrequencyIncreases()
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            MetricType.DeploymentFrequency,
            currentValue: 15m,
            previousValue: 10m);

        result.Direction.Should().Be(MetricTrendDirection.Improving);
    }
    
    [Theory]
    [InlineData(MetricType.CycleTime)]
    [InlineData(MetricType.PRReviewTime)]
    [InlineData(MetricType.ChangeFailureRate)]
    [InlineData(MetricType.LeadTime)]
    [InlineData(MetricType.OpenPRs)]
    [InlineData(MetricType.BlockedItems)]
    public void Calculate_ShouldReturnImproving_WhenLowerIsBetterMetricDecreases(
        MetricType metricType)
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            metricType,
            currentValue: 30m,
            previousValue: 50m);

        result.Direction.Should().Be(MetricTrendDirection.Improving);
    }

    [Theory]
    [InlineData(MetricType.DeploymentFrequency)]
    [InlineData(MetricType.MergedPRs)]
    public void Calculate_ShouldReturnImproving_WhenHigherIsBetterMetricIncreases(
        MetricType metricType)
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            metricType,
            currentValue: 50m,
            previousValue: 30m);

        result.Direction.Should().Be(MetricTrendDirection.Improving);
    }
    
    [Fact]
    public void Calculate_ShouldReturnImproving_WhenHigherIsBetterMetricGoesFromZeroToPositive()
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            MetricType.DeploymentFrequency,
            currentValue: 10m,
            previousValue: 0m);

        result.ChangePercentage.Should().BeNull();
        result.Direction.Should().Be(MetricTrendDirection.Improving);
    }

    [Fact]
    public void Calculate_ShouldReturnDegrading_WhenLowerIsBetterMetricGoesFromZeroToPositive()
    {
        var calculator = new MetricTrendCalculator();

        var result = calculator.Calculate(
            MetricType.BlockedItems,
            currentValue: 10m,
            previousValue: 0m);

        result.ChangePercentage.Should().BeNull();
        result.Direction.Should().Be(MetricTrendDirection.Degrading);
    }
}