using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class BlockedItemsCalculatorTests
{
    private readonly BlockedItemsCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldCountBlockedItems()
    {
        var workItems = new List<JiraWorkItem>
        {
            CreateWorkItem(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreateWorkItem(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 1, 11, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            workItems,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ItemsCount.Should().Be(2);
    }

    [Fact]
    public void Calculate_ShouldIgnoreNonBlockedItems()
    {
        var workItems = new List<JiraWorkItem>
        {
            CreateWorkItem(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreateWorkItem(
                isBlocked: false,
                new DateTimeOffset(
                    2026, 1, 11, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            workItems,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ItemsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldIgnoreItemsOutsidePeriod()
    {
        var workItems = new List<JiraWorkItem>
        {
            CreateWorkItem(
                isBlocked: true,
                new DateTimeOffset(
                    2025, 12, 31, 10, 0, 0, TimeSpan.Zero)),

            CreateWorkItem(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreateWorkItem(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 2, 1, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            workItems,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ItemsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoBlockedItemsExist()
    {
        var workItems = new List<JiraWorkItem>
        {
            CreateWorkItem(
                isBlocked: false,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            workItems,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ItemsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenPeriodIsInvalid()
    {
        var workItems = new List<JiraWorkItem>();

        var action = () => _calculator.Calculate(
            workItems,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 1));

        action.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                "periodStart must be before or equal to periodEnd.");
    }

    private static JiraWorkItem CreateWorkItem(
        bool isBlocked,
        DateTimeOffset createdAt)
    {
        return new JiraWorkItem
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            ExternalId = Guid.NewGuid().ToString(),
            Key = $"REC-{Random.Shared.Next(1, 10000)}",
            Summary = "Test Jira work item",
            Status = isBlocked ? "Blocked" : "In Progress",
            AssigneeExternalId = "test-user",
            CreatedAt = createdAt,
            DoneAt = null,
            IsBlocked = isBlocked
        };
    }
}