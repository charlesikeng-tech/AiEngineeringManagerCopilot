using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Reports;

public sealed class EngineeringActionGeneratorTests
{
    [Fact]
    public void Generate_ShouldCreateOneActionPerInsight()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new[]
        {
            new EngineeringInsight(
                MetricType.PRReviewTime,
                RiskCategory.Review,
                "Reduce PR review time",
                "PR reviews are too slow.",
                "Delivery is impacted.",
                "Review PRs within 12 hours."),

            new EngineeringInsight(
                MetricType.CycleTime,
                RiskCategory.Delivery,
                "Reduce blocked items",
                "Too many blocked items.",
                "Delivery is impacted.",
                "Identify recurring blockers.")
        };

        var actions = generator.Generate(insights,[]);

        actions.Should().HaveCount(2);

        actions[0].Title.Should().Be("Reduce PR review time");
        actions[0].Description
            .Should()
            .Be("Review PRs within 12 hours.");
        actions[0].Priority
            .Should()
            .Be(ActionPriority.High);

        actions[1].Title.Should().Be("Reduce blocked items");
        actions[1].Description
            .Should()
            .Be("Identify recurring blockers.");
        actions[1].Priority
            .Should()
            .Be(ActionPriority.High);
    }
    
    [Fact]
    public void Generate_ShouldReturnEmpty_WhenThereAreNoInsights()
    {
        var generator = new EngineeringActionGenerator();

        var insights = Array.Empty<EngineeringInsight>();

        var actions = generator.Generate(insights,[]);

        actions.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldLimitActionsToThree()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new[]
        {
            CreateInsight("Insight 1"),
            CreateInsight("Insight 2"),
            CreateInsight("Insight 3"),
            CreateInsight("Insight 4"),
            CreateInsight("Insight 5")
        };

        var actions = generator.Generate(insights,[]);

        actions.Should().HaveCount(3);
    }
    
    [Fact]
    public void Generate_ShouldAssignCriticalPriorityToQualityInsights()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new[]
        {
            new EngineeringInsight(
                MetricType.ChangeFailureRate,
                RiskCategory.Quality,
                "Change failure rate is high",
                "Failure rate is too high.",
                "Delivery risk is high.",
                "Strengthen automated testing.")
        };

        var actions = generator.Generate(insights,[]);

        actions.Should().ContainSingle();
        actions[0].Priority
            .Should()
            .Be(ActionPriority.Critical);
    }
    
    [Fact]
    public void Generate_ShouldAssignHighPriorityToDeliveryInsights()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new[]
        {
            new EngineeringInsight(
                MetricType.CycleTime,
                RiskCategory.Delivery,
                "Cycle time is too high",
                "Cycle time is too high.",
                "Delivery is slow.",
                "Break down large pull requests.")
        };

        var actions = generator.Generate(insights,[]);

        actions.Should().ContainSingle();
        actions[0].Priority
            .Should()
            .Be(ActionPriority.High);
    }
    
    [Fact]
    public void Generate_ShouldPrioritizeCriticalActions()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new[]
        {
            new EngineeringInsight(
                MetricType.BlockedItems,
                RiskCategory.Process,
                "Too many items are blocked",
                "Several items are blocked.",
                "Flow is impacted.",
                "Review blocked items."),

            new EngineeringInsight(
                MetricType.ChangeFailureRate,
                RiskCategory.Quality,
                "Change failure rate is high",
                "Failure rate is high.",
                "Delivery risk is high.",
                "Strengthen automated testing."),

            new EngineeringInsight(
                MetricType.CycleTime,
                RiskCategory.Delivery,
                "Cycle time is too high",
                "Cycle time is high.",
                "Delivery is slow.",
                "Break down large pull requests.")
        };

        var actions = generator.Generate(insights,[]);

        actions.Should().HaveCount(3);

        actions[0].Priority
            .Should()
            .Be(ActionPriority.Critical);

        actions[0].Title
            .Should()
            .Be("Change failure rate is high");
    }
    
    [Fact]
    public void Generate_ShouldReturnAtMostThreeActions()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new List<EngineeringInsight>
        {
            new(
                MetricType.CycleTime,
                RiskCategory.Delivery,
                "Insight 1",
                "Evidence",
                "Impact",
                "Recommendation 1"),

            new(
                MetricType.PRReviewTime,
                RiskCategory.Review,
                "Insight 2",
                "Evidence",
                "Impact",
                "Recommendation 2"),

            new(
                MetricType.BlockedItems,
                RiskCategory.Process,
                "Insight 3",
                "Evidence",
                "Impact",
                "Recommendation 3"),

            new(
                MetricType.ChangeFailureRate,
                RiskCategory.Quality,
                "Insight 4",
                "Evidence",
                "Impact",
                "Recommendation 4")
        };

        var actions = generator.Generate(insights,[]);

        actions.Should().HaveCount(3);
    }
    
    [Fact]
    public void Generate_ShouldOrderActionsByPriority()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new List<EngineeringInsight>
        {
            new(
                MetricType.BlockedItems,
                RiskCategory.Process,
                "Process issue",
                "Evidence",
                "Impact",
                "Process recommendation"),

            new(
                MetricType.CycleTime,
                RiskCategory.Delivery,
                "Delivery issue",
                "Evidence",
                "Impact",
                "Delivery recommendation"),

            new(
                MetricType.ChangeFailureRate,
                RiskCategory.Quality,
                "Quality issue",
                "Evidence",
                "Impact",
                "Quality recommendation")
        };

        var actions = generator.Generate(insights,[]);

        actions.Should().HaveCount(3);

        actions[0].Priority.Should().Be(ActionPriority.Critical);
        actions[1].Priority.Should().Be(ActionPriority.High);
        actions[2].Priority.Should().Be(ActionPriority.Medium);
    }
    
    [Fact]
    public void Generate_ShouldPromoteActionToCritical_WhenMatchingRiskIsCritical()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new[]
        {
            new EngineeringInsight(
                MetricType.CycleTime,
                RiskCategory.Delivery,
                "Slow delivery",
                "Delivery is slower than expected.",
                "Features take longer to reach production.",
                "Reduce delivery bottlenecks.")
        };

        var risks = new[]
        {
            new EngineeringRisk
            {
                Id = Guid.NewGuid(),
                TeamId = Guid.NewGuid(),
                ReportId = Guid.NewGuid(),
                MetricType = MetricType.CycleTime,
                Severity = RiskSeverity.Critical,
                Category = RiskCategory.Delivery,
                Title = "Critical delivery risk",
                Description = "Delivery performance is critically degraded.",
                Recommendation = "Investigate delivery bottlenecks.",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var result = generator.Generate(
            insights,
            risks);

        result.Should().ContainSingle();

        result[0].Priority.Should().Be(
            ActionPriority.Critical);
    }
    
    [Fact]
    public void Generate_ShouldNotLowerPriority_WhenMatchingRiskIsLessSevere()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new[]
        {
            new EngineeringInsight(
                MetricType.CycleTime,
                RiskCategory.Delivery,
                "Slow delivery",
                "Delivery is slower than expected.",
                "Features take longer to reach production.",
                "Reduce delivery bottlenecks.")
        };

        var risks = new[]
        {
            new EngineeringRisk
            {
                Id = Guid.NewGuid(),
                TeamId = Guid.NewGuid(),
                ReportId = Guid.NewGuid(),
                Severity = RiskSeverity.Medium,
                Category = RiskCategory.Delivery,
                Title = "Delivery risk",
                Description = "Delivery performance is degraded.",
                Recommendation = "Investigate delivery bottlenecks.",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var result = generator.Generate(
            insights,
            risks);

        result.Should().ContainSingle();

        result[0].Priority.Should().Be(
            ActionPriority.High);
    }
    
    [Fact]
    public void Generate_ShouldUseHighestSeverity_WhenMultipleRisksMatchCategory()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new[]
        {
            new EngineeringInsight(
                MetricType.BlockedItems,
                RiskCategory.Process,
                "Process bottleneck",
                "The engineering process contains bottlenecks.",
                "Delivery may be delayed.",
                "Review and improve the engineering process.")
        };

        var risks = new[]
        {
            new EngineeringRisk
            {
                Id = Guid.NewGuid(),
                TeamId = Guid.NewGuid(),
                ReportId = Guid.NewGuid(),
                Severity = RiskSeverity.Medium,
                Category = RiskCategory.Process,
                Title = "Process risk",
                Description = "A process issue was detected.",
                Recommendation = "Review the process.",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new EngineeringRisk
            {
                Id = Guid.NewGuid(),
                TeamId = Guid.NewGuid(),
                ReportId = Guid.NewGuid(),
                MetricType = MetricType.BlockedItems,
                Severity = RiskSeverity.Critical,
                Category = RiskCategory.Process,
                Title = "Critical process risk",
                Description = "A critical process issue was detected.",
                Recommendation = "Address the bottleneck immediately.",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var result = generator.Generate(
            insights,
            risks);

        result.Should().ContainSingle();

        result[0].Priority.Should().Be(
            ActionPriority.Critical);
    }

    private static EngineeringInsight CreateInsight(string title)
    {
        return new EngineeringInsight(
            MetricType.CycleTime,
            RiskCategory.Delivery,
            title,
            "Description",
            "Impact",
            "Recommendation");
    }
}