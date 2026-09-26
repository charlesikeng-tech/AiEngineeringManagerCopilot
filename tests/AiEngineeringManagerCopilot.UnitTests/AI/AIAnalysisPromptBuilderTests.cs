using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class AIAnalysisPromptBuilderTests
{
    [Fact]
    public void Build_ShouldIncludeMetricsTrendsAndEvidenceInstructions()
    {
        var context = new AIAnalysisContext(
            PeriodStart: new DateOnly(2026, 9, 1),
            PeriodEnd: new DateOnly(2026, 9, 30),
            OverallScore: 72,
            ExecutiveSummary: "Engineering delivery is slowing down.",
            Metrics: new Dictionary<MetricType, decimal>
            {
                [MetricType.CycleTime] = 4.8m
            },
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

        var prompt = sut.Build(context);

        prompt.Should().Contain("2026-09-01");
        prompt.Should().Contain("2026-09-30");
        prompt.Should().Contain("72/100");
        prompt.Should().Contain(
            "Engineering delivery is slowing down.");

        prompt.Should().Contain("- CycleTime: 4.8");

        prompt.Should().Contain(
            "3.2 → 4.8 (+50%)");

        prompt.Should().Contain(
            "metricType must reference a metric present in the provided metrics");

        prompt.Should().Contain(
            "confidence must be between 0 and 1");

        prompt.Should().Contain(
            "Do not invent metrics or metric values");
    }
}