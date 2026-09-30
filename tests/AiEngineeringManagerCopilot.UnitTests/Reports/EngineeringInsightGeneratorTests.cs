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

        var insight = result.Single();

        insight.Category.Should().Be(RiskCategory.Delivery);
        insight.Title.Should().Be(
            "Cycle time is above the healthy range");

        insight.Description.Should().Contain("Average cycle time");
        insight.Description.Should().Contain("48");
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

        var insight = result.Single();

        insight.Category.Should().Be(RiskCategory.Review);
        insight.Title.Should().Be(
            "Pull request review time is above the healthy range");

        insight.Description.Should().Contain("Average first review time");
        insight.Description.Should().Contain("24");
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

        var insight = result.Single();

        insight.Category.Should().Be(RiskCategory.Delivery);
        insight.Title.Should().Be(
            "Recorded deployment frequency is below the healthy range");

        insight.Description.Should().Contain(
            "recorded 2 successful deployments");
    }

    [Fact]
    public void Generate_ShouldTreatZeroDeploymentFrequencyAsRecordedObservation()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.DeploymentFrequency] = 0
        };

        var result = _generator.Generate(metrics);

        result.Should().ContainSingle();

        var insight = result.Single();

        insight.MetricType.Should().Be(
            MetricType.DeploymentFrequency);

        insight.Category.Should().Be(
            RiskCategory.Delivery);

        insight.Title.Should().Be(
            "No deployments were recorded during the period");

        insight.Description.Should().Be(
            "The system recorded 0 successful deployments during the selected period.");

        insight.Impact.Should().Contain(
            "does not establish that no deployments actually occurred");

        insight.Recommendation.Should().Contain(
            "Validate deployment telemetry");
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

        var insight = result.Single();

        insight.Category.Should().Be(RiskCategory.Quality);
        insight.Title.Should().Be(
            "Change failure rate is above the healthy range");

        insight.Description.Should().Contain("Change failure rate");
        insight.Description.Should().Contain("25");
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

        var insight = result.Single();

        insight.Category.Should().Be(RiskCategory.Delivery);
        insight.Title.Should().Be(
            "Lead time is above the healthy range");

        insight.Description.Should().Contain("Average lead time");
        insight.Description.Should().Contain("72");
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

        var insight = result.Single();

        insight.Category.Should().Be(RiskCategory.Review);
        insight.Title.Should().Be(
            "Open pull requests are above the healthy range");

        insight.Description.Should().Contain(
            "10 pull requests are currently open");
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

        var insight = result.Single();

        insight.Category.Should().Be(RiskCategory.Delivery);
        insight.Title.Should().Be(
            "Recorded pull request throughput is below the healthy range");

        insight.Description.Should().Contain(
            "recorded 2 merged pull requests");
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

        var insight = result.Single();

        insight.Category.Should().Be(RiskCategory.Process);
        insight.Title.Should().Be(
            "Blocked items are above the healthy range");

        insight.Description.Should().Contain(
            "5 items are currently recorded as blocked");
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

        result.Select(x => x.MetricType)
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    MetricType.CycleTime,
                    MetricType.PRReviewTime,
                    MetricType.DeploymentFrequency,
                    MetricType.ChangeFailureRate,
                    MetricType.LeadTime,
                    MetricType.OpenPRs,
                    MetricType.MergedPRs,
                    MetricType.BlockedItems
                });
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
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.DeploymentFrequency] = 7m
        };

        var insights = _generator.Generate(metrics);

        insights.Should().ContainSingle();

        var insight = insights.Single();

        insight.MetricType.Should().Be(
            MetricType.DeploymentFrequency);

        insight.Category.Should().Be(
            RiskCategory.Delivery);

        insight.Title.Should().Be(
            "Recorded deployment frequency is below the healthy range");

        insight.Description.Should().Contain(
            "recorded 7 successful deployments");
    }

    [Fact]
    public void Generate_ShouldCreateMergedPullRequestsInsight_WhenThroughputNeedsAttention()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.MergedPRs] = 7m
        };

        var insights = _generator.Generate(metrics);

        insights.Should().ContainSingle();

        var insight = insights.Single();

        insight.MetricType.Should().Be(
            MetricType.MergedPRs);

        insight.Category.Should().Be(
            RiskCategory.Delivery);

        insight.Title.Should().Be(
            "Recorded pull request throughput is below the healthy range");

        insight.Description.Should().Contain(
            "recorded 7 merged pull requests");
    }
}