using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Risks;

public sealed class EngineeringRiskDetectorTests
{
    private readonly EngineeringRiskDetector _detector = new();

    private static readonly Guid TeamId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid ReportId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenCycleTimeIsAbove48Hours()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 49
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.TeamId.Should().Be(TeamId);
        risk.ReportId.Should().Be(ReportId);
        risk.MetricType.Should().Be(MetricType.CycleTime);
        risk.Category.Should().Be(RiskCategory.Delivery);
        risk.Severity.Should().Be(RiskSeverity.High);
        risk.Title.Should().Be("High cycle time");
    }

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenReviewTimeIsAbove24Hours()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.PRReviewTime] = 25
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.MetricType.Should().Be(MetricType.PRReviewTime);
        risk.Category.Should().Be(RiskCategory.Review);
        risk.Severity.Should().Be(RiskSeverity.High);
        risk.Title.Should().Be("Slow pull request reviews");

        risk.Recommendation.Should().Contain(
            "define a review-time expectation");
    }

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenDeploymentFrequencyIsBelow5()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.DeploymentFrequency] = 4
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.MetricType.Should().Be(
            MetricType.DeploymentFrequency);

        risk.Category.Should().Be(
            RiskCategory.Delivery);

        risk.Severity.Should().Be(
            RiskSeverity.High);

        risk.Title.Should().Be(
            "Low recorded deployment frequency");

        risk.Description.Should().Contain(
            "recorded 4 successful deployments");
    }

    [Fact]
    public void Detect_ShouldCreateCriticalRisk_WhenChangeFailureRateIsAbove20Percent()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.ChangeFailureRate] = 21
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.MetricType.Should().Be(
            MetricType.ChangeFailureRate);

        risk.Category.Should().Be(
            RiskCategory.Reliability);

        risk.Severity.Should().Be(
            RiskSeverity.Critical);

        risk.Title.Should().Be(
            "High change failure rate");
    }

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenLeadTimeIsAbove72Hours()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.LeadTime] = 73
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.MetricType.Should().Be(MetricType.LeadTime);
        risk.Category.Should().Be(RiskCategory.Delivery);
        risk.Severity.Should().Be(RiskSeverity.High);
        risk.Title.Should().Be("High lead time");
    }

    [Fact]
    public void Detect_ShouldCreateMediumRisk_WhenOpenPullRequestsAreAbove10()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.OpenPRs] = 11
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.MetricType.Should().Be(MetricType.OpenPRs);
        risk.Category.Should().Be(RiskCategory.Review);
        risk.Severity.Should().Be(RiskSeverity.Medium);
        risk.Title.Should().Be("Too many open pull requests");
    }

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenMergedPullRequestsAreBelow5()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.MergedPRs] = 4
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.MetricType.Should().Be(MetricType.MergedPRs);
        risk.Category.Should().Be(RiskCategory.Delivery);
        risk.Severity.Should().Be(RiskSeverity.High);
        risk.Title.Should().Be("Low delivery throughput");
    }

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenBlockedItemsAreAbove5()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.BlockedItems] = 6
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.MetricType.Should().Be(MetricType.BlockedItems);
        risk.Category.Should().Be(RiskCategory.Process);
        risk.Severity.Should().Be(RiskSeverity.High);
        risk.Title.Should().Be("Too many blocked items");
    }

    [Fact]
    public void Detect_ShouldNotCreateRisk_WhenMetricsAreHealthy()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 48,
            [MetricType.PRReviewTime] = 24,
            [MetricType.DeploymentFrequency] = 5,
            [MetricType.ChangeFailureRate] = 20,
            [MetricType.LeadTime] = 72,
            [MetricType.OpenPRs] = 10,
            [MetricType.MergedPRs] = 5,
            [MetricType.BlockedItems] = 5
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().BeEmpty();
    }

    [Fact]
    public void Detect_ShouldCreateMultipleRisks_WhenMultipleMetricsAreUnhealthy()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 72,
            [MetricType.PRReviewTime] = 48,
            [MetricType.DeploymentFrequency] = 2,
            [MetricType.ChangeFailureRate] = 25,
            [MetricType.LeadTime] = 96,
            [MetricType.OpenPRs] = 15,
            [MetricType.MergedPRs] = 2,
            [MetricType.BlockedItems] = 8
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().HaveCount(8);

        risks.Select(risk => risk.MetricType)
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

        risks.Should().Contain(r =>
            r.Severity == RiskSeverity.Critical &&
            r.Category == RiskCategory.Reliability);

        risks.Should().Contain(r =>
            r.Severity == RiskSeverity.Medium &&
            r.Category == RiskCategory.Review);
    }

    [Fact]
    public void Detect_ShouldIgnoreMissingMetrics()
    {
        var metrics =
            new Dictionary<MetricType, decimal>();

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().BeEmpty();
    }

    [Fact]
    public void Detect_ShouldReturnNoRisks_WhenNoMetricsAreAvailable()
    {
        var metrics =
            new Dictionary<MetricType, decimal>();

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().BeEmpty();
    }

    [Fact]
    public void Detect_ShouldTreatZeroDeploymentFrequencyAsRecordedObservation()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.DeploymentFrequency] = 0m
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.TeamId.Should().Be(TeamId);
        risk.ReportId.Should().Be(ReportId);

        risk.MetricType.Should().Be(
            MetricType.DeploymentFrequency);

        risk.Severity.Should().Be(
            RiskSeverity.High);

        risk.Category.Should().Be(
            RiskCategory.Delivery);

        risk.Title.Should().Be(
            "No deployments were recorded during the period");

        risk.Description.Should().Contain(
            "recorded 0 successful deployments");

        risk.Description.Should().Contain(
            "does not establish that no deployments actually occurred");

        risk.Recommendation.Should().Contain(
            "Validate deployment telemetry");
    }

    [Fact]
    public void Detect_ShouldDetectLowDeliveryThroughput_WhenMergedPullRequestsIsZero()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.MergedPRs] = 0m
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.MetricType.Should().Be(MetricType.MergedPRs);
        risk.Severity.Should().Be(RiskSeverity.High);
        risk.Category.Should().Be(RiskCategory.Delivery);
        risk.Title.Should().Be("Low delivery throughput");
    }

    [Fact]
    public void Detect_ShouldNotCreateDeploymentRisk_WhenFrequencyNeedsAttention()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.DeploymentFrequency] = 7m
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().BeEmpty();
    }

    [Fact]
    public void Detect_ShouldNotCreateMergedPullRequestsRisk_WhenThroughputNeedsAttention()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.MergedPRs] = 7m
        };

        var risks = _detector.Detect(
            TeamId,
            ReportId,
            metrics);

        risks.Should().BeEmpty();
    }
}