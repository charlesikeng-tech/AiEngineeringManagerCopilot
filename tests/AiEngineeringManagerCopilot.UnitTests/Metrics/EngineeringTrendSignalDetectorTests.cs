using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class EngineeringTrendSignalDetectorTests
{
    private readonly EngineeringTrendSignalDetector _detector = new();

    [Fact]
    public void Detect_ShouldReturnNull_WhenDegradationIsBelowThreshold()
    {
        var trend = new MetricTrendResult(
            MetricType.CycleTime,
            11m,
            10m,
            10m,
            MetricTrendDirection.Degrading);

        _detector.Detect(trend)
            .Should()
            .BeNull();
    }

    [Fact]
    public void Detect_ShouldReturnSignal_WhenDegradationReachesThreshold()
    {
        var trend = new MetricTrendResult(
            MetricType.CycleTime,
            12m,
            10m,
            20m,
            MetricTrendDirection.Degrading);

        var result = _detector.Detect(trend);

        result.Should().NotBeNull();
        result!.MetricType.Should().Be(MetricType.CycleTime);
        result.Direction.Should().Be(MetricTrendDirection.Degrading);
        result.ChangePercentage.Should().Be(20m);
    }

    [Fact]
    public void Detect_ShouldReturnSignal_WhenDegradationIsAboveThreshold()
    {
        var trend = new MetricTrendResult(
            MetricType.CycleTime,
            15m,
            10m,
            50m,
            MetricTrendDirection.Degrading);

        _detector.Detect(trend)
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void Detect_ShouldReturnNull_WhenTrendIsImproving()
    {
        var trend = new MetricTrendResult(
            MetricType.CycleTime,
            6m,
            10m,
            -40m,
            MetricTrendDirection.Improving);

        _detector.Detect(trend)
            .Should()
            .BeNull();
    }

    [Fact]
    public void Detect_ShouldReturnNull_WhenTrendIsStable()
    {
        var trend = new MetricTrendResult(
            MetricType.CycleTime,
            10m,
            10m,
            0m,
            MetricTrendDirection.Stable);

        _detector.Detect(trend)
            .Should()
            .BeNull();
    }

    [Fact]
    public void Detect_ShouldHandleNegativePercentageForHigherIsBetterMetric()
    {
        var trend = new MetricTrendResult(
            MetricType.MergedPRs,
            7m,
            10m,
            -30m,
            MetricTrendDirection.Degrading);

        _detector.Detect(trend)
            .Should()
            .NotBeNull();
    }
    
    [Fact]
    public void Detect_ShouldReturnSignal_WhenDegradingFromZero()
    {
        var trend = new MetricTrendResult(
            MetricType.BlockedItems,
            5m,
            0m,
            null,
            MetricTrendDirection.Degrading);

        var result = _detector.Detect(trend);

        result.Should().NotBeNull();
        result!.MetricType.Should().Be(MetricType.BlockedItems);
        result.Direction.Should().Be(MetricTrendDirection.Degrading);
        result.ChangePercentage.Should().BeNull();
    }

    [Fact]
    public void Detect_ShouldReturnNull_WhenImprovingWithUndefinedPercentage()
    {
        var trend = new MetricTrendResult(
            MetricType.DeploymentFrequency,
            10m,
            0m,
            null,
            MetricTrendDirection.Improving);

        _detector.Detect(trend)
            .Should()
            .BeNull();
    }
    
    [Fact]
    public void Generate_ShouldCreateEarlyWarningInsight()
    {
        var generator =
            new EngineeringTrendInsightGenerator();

        var signal = new EngineeringTrendSignal(
            MetricType.CycleTime,
            MetricTrendDirection.Degrading,
            50m);

        var result = generator.Generate(signal);

        result.MetricType
            .Should()
            .Be(MetricType.CycleTime);

        result.Category
            .Should()
            .Be(RiskCategory.Delivery);

        result.Title
            .Should()
            .Contain("Early warning");

        result.Description
            .Should()
            .Contain("50%");
    }
    
    [Fact]
    public void Generate_ShouldCreateEarlyWarning_WhenNoExistingInsightExists()
    {
        var service = new EngineeringTrendInsightService(
            new EngineeringTrendSignalDetector(),
            new EngineeringTrendInsightGenerator());

        var trends = new[]
        {
            new MetricTrendResult(
                MetricType.CycleTime,
                20m,
                10m,
                100m,
                MetricTrendDirection.Degrading)
        };

        var result = service.Generate(
            trends,
            []);

        result.Should().ContainSingle();

        result[0].MetricType
            .Should()
            .Be(MetricType.CycleTime);

        result[0].Title
            .Should()
            .Contain("Early warning");
    }
    
    [Fact]
    public void Generate_ShouldNotCreateEarlyWarning_WhenInsightAlreadyExistsForMetric()
    {
        var service = new EngineeringTrendInsightService(
            new EngineeringTrendSignalDetector(),
            new EngineeringTrendInsightGenerator());

        var trends = new[]
        {
            new MetricTrendResult(
                MetricType.CycleTime,
                30m,
                10m,
                200m,
                MetricTrendDirection.Degrading)
        };

        var existingInsights = new[]
        {
            new EngineeringInsight(
                MetricType.CycleTime,
                RiskCategory.Delivery,
                "Cycle time is too high",
                "Cycle time exceeded the expected threshold.",
                "Delivery may slow down.",
                "Investigate the delivery workflow.")
        };

        var result = service.Generate(
            trends,
            existingInsights);

        result.Should().BeEmpty();
    }
}