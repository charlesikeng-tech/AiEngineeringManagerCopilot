using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class LeadTimeCalculatorTests
{
    private readonly LeadTimeCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldReturnAverageLeadTime()
    {
        var periodStart = new DateOnly(2026, 1, 1);
        var periodEnd = new DateOnly(2026, 1, 31);

        var workItems = new[]
        {
            CreateWorkItem(
                new DateTimeOffset(
                    2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 1, 10, 18, 0, 0, TimeSpan.Zero)),

            CreateWorkItem(
                new DateTimeOffset(
                    2026, 1, 11, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 1, 12, 4, 0, 0, TimeSpan.Zero)),

            CreateWorkItem(
                new DateTimeOffset(
                    2026, 1, 13, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 1, 13, 23, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            workItems,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(15);
        result.ItemsCount.Should().Be(3);
    }

    [Fact]
    public void Calculate_ShouldIgnoreIncompleteWorkItems()
    {
        var periodStart = new DateOnly(2026, 1, 1);
        var periodEnd = new DateOnly(2026, 1, 31);

        var workItems = new[]
        {
            CreateWorkItem(
                new DateTimeOffset(
                    2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
                null),

            CreateWorkItem(
                new DateTimeOffset(
                    2026, 1, 11, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 1, 11, 18, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            workItems,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(10);
        result.ItemsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldIgnoreWorkItemsOutsidePeriod()
    {
        var periodStart = new DateOnly(2026, 1, 1);
        var periodEnd = new DateOnly(2026, 1, 31);

        var workItems = new[]
        {
            CreateWorkItem(
                new DateTimeOffset(
                    2025, 12, 31, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(
                    2025, 12, 31, 18, 0, 0, TimeSpan.Zero)),

            CreateWorkItem(
                new DateTimeOffset(
                    2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 1, 10, 18, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            workItems,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(10);
        result.ItemsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoEligibleWorkItemsExist()
    {
        var periodStart = new DateOnly(2026, 1, 1);
        var periodEnd = new DateOnly(2026, 1, 31);

        var workItems = new[]
        {
            CreateWorkItem(
                new DateTimeOffset(
                    2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
                null)
        };

        var result = _calculator.Calculate(
            workItems,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(0);
        result.ItemsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenPeriodIsInvalid()
    {
        var periodStart = new DateOnly(2026, 2, 1);
        var periodEnd = new DateOnly(2026, 1, 1);

        var act = () => _calculator.Calculate(
            Array.Empty<JiraWorkItem>(),
            periodStart,
            periodEnd);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                "periodStart must be before or equal to periodEnd.");
    }

    private static JiraWorkItem CreateWorkItem(
        DateTimeOffset createdAt,
        DateTimeOffset? doneAt)
    {
        return new JiraWorkItem
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            ExternalId = Guid.NewGuid().ToString(),
            Key = $"REC-{Random.Shared.Next(1, 100000)}",
            Summary = "Test Jira work item",
            Status = doneAt.HasValue
                ? "Done"
                : "In Progress",
            AssigneeExternalId = "jira-user-1",
            CreatedAt = createdAt,
            DoneAt = doneAt,
            IsBlocked = false
        };
    }
}