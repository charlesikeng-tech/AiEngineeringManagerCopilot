using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class MetricTrendBuilderTests
{
    [Fact]
    public void Build_ShouldCreateTrendForMatchingMetric()
    {
        var trendCalculator = new MetricTrendCalculator();
        var builder = new MetricTrendBuilder(trendCalculator);

        var currentMetrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.CycleTime] = 30m
            };

        var previousMetrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.CycleTime] = 50m
            };

        var result = builder.Build(
            currentMetrics,
            previousMetrics);

        result.Should().ContainSingle();

        var trend = result.Single();

        trend.MetricType.Should().Be(MetricType.CycleTime);
        trend.CurrentValue.Should().Be(30m);
        trend.PreviousValue.Should().Be(50m);
        trend.ChangePercentage.Should().Be(-40m);
        trend.Direction.Should().Be(
            MetricTrendDirection.Improving);
    }
    
    [Fact]
    public void Build_ShouldIgnoreMetric_WhenPreviousMetricIsMissing()
    {
        var trendCalculator = new MetricTrendCalculator();
        var builder = new MetricTrendBuilder(trendCalculator);

        var currentMetrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.CycleTime] = 30m
            };

        var previousMetrics =
            new Dictionary<MetricType, decimal>();

        var result = builder.Build(
            currentMetrics,
            previousMetrics);

        result.Should().BeEmpty();
    }
    
    [Fact]
    public void Build_ShouldIgnoreMetric_WhenCurrentMetricIsMissing()
    {
        var trendCalculator = new MetricTrendCalculator();
        var builder = new MetricTrendBuilder(trendCalculator);

        var currentMetrics =
            new Dictionary<MetricType, decimal>();

        var previousMetrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.CycleTime] = 50m
            };

        var result = builder.Build(
            currentMetrics,
            previousMetrics);

        result.Should().BeEmpty();
    }
}