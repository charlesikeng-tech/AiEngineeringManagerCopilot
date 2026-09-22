using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Reports;

public sealed class EngineeringInsightGeneratorTests
{
    private readonly EngineeringInsightGenerator _generator = new();

    [Fact]
    public void Generate_ShouldReturnNoInsights_WhenAllMetricsAreHealthy()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 8,
            [MetricType.PRReviewTime] = 4,
            [MetricType.DeploymentFrequency] = 20,
            [MetricType.ChangeFailureRate] = 5,
            [MetricType.LeadTime] = 24,
            [MetricType.OpenPRs] = 2,
            [MetricType.MergedPRs] = 20,
            [MetricType.BlockedItems] = 0
        };

        var result = _generator.Generate(metrics);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldDetectCycleTimeProblem()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 48
        };

        var result = _generator.Generate(metrics);

        result.Should().ContainSingle();

        result[0].Category.Should().Be(RiskCategory.Delivery);
        result[0].Title.Should().Be("Cycle time is too high");
    }

    [Fact]
    public void Generate_ShouldDetectReviewTimeProblem()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.PRReviewTime] = 24
        };

        var result = _generator.Generate(metrics);

        result.Should().ContainSingle();

        result[0].Category.Should().Be(RiskCategory.Review);
        result[0].Title.Should().Be(
            "Pull request review time is high");
    }

    [Fact]
    public void Generate_ShouldDetectDeploymentFrequencyProblem()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.DeploymentFrequency] = 2
        };

        var result = _generator.Generate(metrics);

        result.Should().ContainSingle();

        result[0].Title.Should().Be(
            "Deployment frequency is low");
    }

    [Fact]
    public void Generate_ShouldDetectChangeFailureRateProblem()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.ChangeFailureRate] = 25
        };

        var result = _generator.Generate(metrics);

        result.Should().ContainSingle();

        result[0].Category.Should().Be(RiskCategory.Quality);
        result[0].Title.Should().Be(
            "Change failure rate is high");
    }

    [Fact]
    public void Generate_ShouldDetectLeadTimeProblem()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.LeadTime] = 72
        };

        var result = _generator.Generate(metrics);

        result.Should().ContainSingle();

        result[0].Title.Should().Be(
            "Lead time is high");
    }

    [Fact]
    public void Generate_ShouldDetectOpenPullRequestsProblem()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.OpenPRs] = 10
        };

        var result = _generator.Generate(metrics);

        result.Should().ContainSingle();

        result[0].Category.Should().Be(RiskCategory.Review);
        result[0].Title.Should().Be(
            "Too many pull requests are open");
    }

    [Fact]
    public void Generate_ShouldDetectLowPullRequestThroughput()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.MergedPRs] = 2
        };

        var result = _generator.Generate(metrics);

        result.Should().ContainSingle();

        result[0].Title.Should().Be(
            "Low pull request throughput");
    }

    [Fact]
    public void Generate_ShouldDetectBlockedItemsProblem()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.BlockedItems] = 5
        };

        var result = _generator.Generate(metrics);

        result.Should().ContainSingle();

        result[0].Category.Should().Be(RiskCategory.Process);
        result[0].Title.Should().Be(
            "Too many items are blocked");
    }

    [Fact]
    public void Generate_ShouldReturnMultipleInsights_WhenMultipleMetricsAreBad()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 72,
            [MetricType.PRReviewTime] = 48,
            [MetricType.DeploymentFrequency] = 1,
            [MetricType.ChangeFailureRate] = 30,
            [MetricType.LeadTime] = 168,
            [MetricType.OpenPRs] = 20,
            [MetricType.MergedPRs] = 1,
            [MetricType.BlockedItems] = 10
        };

        var result = _generator.Generate(metrics);

        result.Should().HaveCount(8);
    }

    [Fact]
    public void Generate_ShouldIgnoreMissingMetrics()
    {
        var metrics = new Dictionary<MetricType, decimal>();

        var result = _generator.Generate(metrics);

        result.Should().BeEmpty();
    }
    
    [Fact]
    public void Generate_ShouldCreateDeploymentInsight_WhenFrequencyNeedsAttention()
    {
        var generator = new EngineeringInsightGenerator();

        var metrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.DeploymentFrequency] = 7m
            };

        var insights = generator.Generate(metrics);

        insights.Should().ContainSingle();

        var insight = insights.Single();

        insight.Category.Should().Be(RiskCategory.Delivery);
        insight.Title.Should().Be("Deployment frequency is low");
    }
    
    [Fact]
    public void Generate_ShouldCreateMergedPullRequestsInsight_WhenThroughputNeedsAttention()
    {
        var generator = new EngineeringInsightGenerator();

        var metrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.MergedPRs] = 7m
            };

        var insights = generator.Generate(metrics);

        insights.Should().ContainSingle();

        var insight = insights.Single();

        insight.Category.Should().Be(RiskCategory.Delivery);
        insight.Title.Should().Be("Low pull request throughput");
    }
}