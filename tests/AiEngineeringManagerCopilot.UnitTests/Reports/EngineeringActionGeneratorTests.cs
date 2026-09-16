using AiEngineeringManagerCopilot.Application.Reports;
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
                "Review",
                "Reduce PR review time",
                "PR reviews are too slow.",
                "Delivery is impacted.",
                "Review PRs within 12 hours."),

            new EngineeringInsight(
                "Delivery",
                "Reduce blocked items",
                "Too many blocked items.",
                "Delivery is impacted.",
                "Identify recurring blockers.")
        };

        var actions = generator.Generate(insights);

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

        var actions = generator.Generate(insights);

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

        var actions = generator.Generate(insights);

        actions.Should().HaveCount(3);
    }
    
    [Fact]
    public void Generate_ShouldAssignCriticalPriorityToQualityInsights()
    {
        var generator = new EngineeringActionGenerator();

        var insights = new[]
        {
            new EngineeringInsight(
                "Quality",
                "Change failure rate is high",
                "Failure rate is too high.",
                "Delivery risk is high.",
                "Strengthen automated testing.")
        };

        var actions = generator.Generate(insights);

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
                "Delivery",
                "Cycle time is too high",
                "Cycle time is too high.",
                "Delivery is slow.",
                "Break down large pull requests.")
        };

        var actions = generator.Generate(insights);

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
                "Process",
                "Too many items are blocked",
                "Several items are blocked.",
                "Flow is impacted.",
                "Review blocked items."),

            new EngineeringInsight(
                "Quality",
                "Change failure rate is high",
                "Failure rate is high.",
                "Delivery risk is high.",
                "Strengthen automated testing."),

            new EngineeringInsight(
                "Delivery",
                "Cycle time is too high",
                "Cycle time is high.",
                "Delivery is slow.",
                "Break down large pull requests.")
        };

        var actions = generator.Generate(insights);

        actions.Should().HaveCount(3);

        actions[0].Priority
            .Should()
            .Be(ActionPriority.Critical);

        actions[0].Title
            .Should()
            .Be("Change failure rate is high");
    }

    private static EngineeringInsight CreateInsight(string title)
    {
        return new EngineeringInsight(
            "Delivery",
            title,
            "Description",
            "Impact",
            "Recommendation");
    }
}