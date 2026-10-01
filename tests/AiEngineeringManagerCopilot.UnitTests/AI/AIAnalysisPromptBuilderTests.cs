using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class AIAnalysisPromptBuilderTests
{
    [Fact]
    public void Build_ShouldIncludeMetricsDataCoverageTrendsAndEvidenceInstructions()
    {
        // Arrange
        var context = new AIAnalysisContext(
            PeriodStart: new DateOnly(2026, 9, 1),
            PeriodEnd: new DateOnly(2026, 9, 30),
            OverallScore: 72,
            DataCoverage: 45m,
            ExecutiveSummary: "Engineering delivery is slowing down.",
            Metrics:
            [
                new AIAnalysisMetricContext(
                    MetricType.CycleTime,
                    4.8m,
                    MetricDataStatus.Available),

                new AIAnalysisMetricContext(
                    MetricType.DeploymentFrequency,
                    0m,
                    MetricDataStatus.Available),

                new AIAnalysisMetricContext(
                    MetricType.PRReviewTime,
                    null,
                    MetricDataStatus.NoData),

                new AIAnalysisMetricContext(
                    MetricType.LeadTime,
                    null,
                    MetricDataStatus.SourceNotConfigured)
            ],
            Insights: [],
            Risks: [],
            Trends:
            [
                new MetricTrendResult(
                    MetricType.CycleTime,
                    CurrentValue: 4.8m,
                    PreviousValue: 3.2m,
                    ChangePercentage: 50m,
                    Direction: MetricTrendDirection.Degrading)
            ]);

        var sut = new AIAnalysisPromptBuilder();

        // Act
        var prompt = sut.Build(context);

        // Assert
        prompt.Should().Contain("2026-09-01");
        prompt.Should().Contain("2026-09-30");

        prompt.Should().Contain("72/100");
        prompt.Should().Contain("Data coverage: 45%");

        prompt.Should().Contain(
            "Engineering delivery is slowing down.");

        // Available metrics
        prompt.Should().Contain("- CycleTime: 4.8");

        // A legitimate zero must remain visible.
        prompt.Should().Contain("- DeploymentFrequency: 0");

        // Unavailable metrics
        prompt.Should().Contain("- PRReviewTime: NoData");
        prompt.Should().Contain(
            "- LeadTime: SourceNotConfigured");

        // Trends
        prompt.Should().Contain(
            "3.2 → 4.8 (+50%)");

        // Data semantics
        prompt.Should().Contain(
            "SourceNotConfigured means");

        prompt.Should().Contain(
            "NoData means");

        prompt.Should().Contain(
            "A metric marked Available with value 0");

        prompt.Should().Contain(
            "Never use NoData or SourceNotConfigured metrics as evidence");

        // Evidence safety
        prompt.Should().Contain(
            "metricType must reference an Available metric");

        prompt.Should().Contain(
            "confidence must be between 0 and 1");

        prompt.Should().Contain(
            "Do not invent metrics, metric values, targets, units, benchmarks");
    }
    
    [Fact]
    public void Build_ShouldDescribeBlockedItemsAsCurrentSnapshot()
    {
        // Arrange
        var context = new AIAnalysisContext(
            PeriodStart: new DateOnly(2026, 9, 1),
            PeriodEnd: new DateOnly(2026, 9, 30),
            OverallScore: 79,
            DataCoverage: 70m,
            ExecutiveSummary: "Engineering health is Healthy.",
            Metrics:
            [
                new AIAnalysisMetricContext(
                    MetricType.BlockedItems,
                    2m,
                    MetricDataStatus.Available,
                    "items",
                    MetricTemporalSemantics.CurrentSnapshot,
                    "Number of Jira work items whose current status is Blocked.")
            ],
            Insights: [],
            Risks: [],
            Trends: []);

        var sut = new AIAnalysisPromptBuilder();

        // Act
        var prompt = sut.Build(context);

        // Assert
        prompt.Should().Contain("BlockedItems: 2 items");
        prompt.Should().Contain("CurrentSnapshot");
        prompt.Should().Contain(
            "Number of Jira work items whose current status is Blocked.");

        prompt.Should().Contain(
            "Do not infer historical state or duration from a CurrentSnapshot metric");
    }
    
    [Fact]
    public void Build_ShouldIncludeLeadTimeUnitAndTemporalSemantics()
    {
        // Arrange
        var context = new AIAnalysisContext(
            PeriodStart: new DateOnly(2026, 9, 1),
            PeriodEnd: new DateOnly(2026, 9, 30),
            OverallScore: 79,
            DataCoverage: 70m,
            ExecutiveSummary: "Engineering health is Healthy.",
            Metrics:
            [
                new AIAnalysisMetricContext(
                    MetricType.LeadTime,
                    0.03m,
                    MetricDataStatus.Available,
                    "hours",
                    MetricTemporalSemantics.ReportingPeriod,
                    "Average time from Jira issue creation to resolution " +
                    "for issues completed during the reporting period.")
            ],
            Insights: [],
            Risks: [],
            Trends: []);

        var sut = new AIAnalysisPromptBuilder();

        // Act
        var prompt = sut.Build(context);

        // Assert
        prompt.Should().Contain("LeadTime: 0.03 hours");
        prompt.Should().Contain("ReportingPeriod");
        prompt.Should().Contain(
            "Average time from Jira issue creation to resolution");
    }
}