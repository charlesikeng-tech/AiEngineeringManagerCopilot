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

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().ContainSingle();

        var risk = risks[0];

        risk.TeamId.Should().Be(TeamId);
        risk.ReportId.Should().Be(ReportId);
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

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().ContainSingle();
        risks[0].Category.Should().Be(RiskCategory.Review);
        risks[0].Severity.Should().Be(RiskSeverity.High);
    }

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenDeploymentFrequencyIsBelow5()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.DeploymentFrequency] = 4
        };

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().ContainSingle();
        risks[0].Category.Should().Be(RiskCategory.Delivery);
        risks[0].Severity.Should().Be(RiskSeverity.High);
    }

    [Fact]
    public void Detect_ShouldCreateCriticalRisk_WhenChangeFailureRateIsAbove20Percent()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.ChangeFailureRate] = 21
        };

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().ContainSingle();
        risks[0].Category.Should().Be(RiskCategory.Reliability);
        risks[0].Severity.Should().Be(RiskSeverity.Critical);
    }

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenLeadTimeIsAbove72Hours()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.LeadTime] = 73
        };

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().ContainSingle();
        risks[0].Category.Should().Be(RiskCategory.Delivery);
        risks[0].Severity.Should().Be(RiskSeverity.High);
    }

    [Fact]
    public void Detect_ShouldCreateMediumRisk_WhenOpenPullRequestsAreAbove10()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.OpenPRs] = 11
        };

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().ContainSingle();
        risks[0].Category.Should().Be(RiskCategory.Review);
        risks[0].Severity.Should().Be(RiskSeverity.Medium);
    }

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenMergedPullRequestsAreBelow5()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.MergedPRs] = 4
        };

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().ContainSingle();
        risks[0].Category.Should().Be(RiskCategory.Delivery);
        risks[0].Severity.Should().Be(RiskSeverity.High);
    }

    [Fact]
    public void Detect_ShouldCreateHighRisk_WhenBlockedItemsAreAbove5()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.BlockedItems] = 6
        };

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().ContainSingle();
        risks[0].Category.Should().Be(RiskCategory.Process);
        risks[0].Severity.Should().Be(RiskSeverity.High);
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

        var risks = _detector.Detect(TeamId, ReportId, metrics);

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

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().HaveCount(8);

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
        var metrics = new Dictionary<MetricType, decimal>();

        var risks = _detector.Detect(TeamId, ReportId, metrics);

        risks.Should().BeEmpty();
    }
    
    [Fact]
    public void Detect_ShouldReturnNoRisks_WhenNoMetricsAreAvailable()
    {
        var detector = new EngineeringRiskDetector();

        var metrics =
            new Dictionary<MetricType, decimal>();

        var risks = detector.Detect(
            Guid.NewGuid(),
            Guid.NewGuid(),
            metrics);

        risks.Should().BeEmpty();
    }
    
    [Fact]
    public void Detect_ShouldDetectLowDeploymentFrequency_WhenValueIsZero()
    {
        var detector = new EngineeringRiskDetector();

        var metrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.DeploymentFrequency] = 0m
            };

        var risks = detector.Detect(
            Guid.NewGuid(),
            Guid.NewGuid(),
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.Severity.Should().Be(RiskSeverity.High);
        risk.Category.Should().Be(RiskCategory.Delivery);
        risk.Title.Should().Be("Low deployment frequency");
    }
    
    [Fact]
    public void Detect_ShouldDetectLowDeliveryThroughput_WhenMergedPullRequestsIsZero()
    {
        var detector = new EngineeringRiskDetector();

        var metrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.MergedPRs] = 0m
            };

        var risks = detector.Detect(
            Guid.NewGuid(),
            Guid.NewGuid(),
            metrics);

        risks.Should().ContainSingle();

        var risk = risks.Single();

        risk.Severity.Should().Be(RiskSeverity.High);
        risk.Category.Should().Be(RiskCategory.Delivery);
        risk.Title.Should().Be("Low delivery throughput");
    }
    
    [Fact]
    public void Detect_ShouldNotCreateDeploymentRisk_WhenFrequencyNeedsAttention()
    {
        var detector = new EngineeringRiskDetector();

        var metrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.DeploymentFrequency] = 7m
            };

        var risks = detector.Detect(
            Guid.NewGuid(),
            Guid.NewGuid(),
            metrics);

        risks.Should().BeEmpty();
    }
    
    [Fact]
    public void Detect_ShouldNotCreateMergedPullRequestsRisk_WhenThroughputNeedsAttention()
    {
        var detector = new EngineeringRiskDetector();

        var metrics =
            new Dictionary<MetricType, decimal>
            {
                [MetricType.MergedPRs] = 7m
            };

        var risks = detector.Detect(
            Guid.NewGuid(),
            Guid.NewGuid(),
            metrics);

        risks.Should().BeEmpty();
    }
}