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
    public void Calculate_ShouldCountBlockedItemsRegardlessOfCreationDate()
    {
        // Arrange
        var workItems = new[]
        {
            new JiraWorkItem
            {
                CreatedAt = new DateTimeOffset(
                    2026, 8, 15, 10, 0, 0, TimeSpan.Zero),
                IsBlocked = true
            },
            new JiraWorkItem
            {
                CreatedAt = new DateTimeOffset(
                    2026, 9, 15, 10, 0, 0, TimeSpan.Zero),
                IsBlocked = true
            },
            new JiraWorkItem
            {
                CreatedAt = new DateTimeOffset(
                    2026, 10, 15, 10, 0, 0, TimeSpan.Zero),
                IsBlocked = true
            },
            new JiraWorkItem
            {
                CreatedAt = new DateTimeOffset(
                    2026, 9, 20, 10, 0, 0, TimeSpan.Zero),
                IsBlocked = false
            }
        };

        var sut = new BlockedItemsCalculator();

        // Act
        var result = sut.Calculate(
            workItems,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        // Assert
        result.ItemsCount.Should().Be(3);
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
    
    [Fact]
    public void Calculate_ShouldCountBlockedItemCreatedBeforePeriod()
    {
        var workItems = new[]
        {
            new JiraWorkItem
            {
                CreatedAt = new DateTimeOffset(
                    2026, 8, 15, 10, 0, 0, TimeSpan.Zero),
                IsBlocked = true
            }
        };

        var result = _calculator.Calculate(
            workItems,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        Assert.Equal(1, result.ItemsCount);
    }
    
    [Fact]
    public void Calculate_ShouldIgnoreItemsThatAreNotBlocked()
    {
        var workItems = new[]
        {
            new JiraWorkItem
            {
                CreatedAt = DateTimeOffset.UtcNow,
                IsBlocked = true
            },
            new JiraWorkItem
            {
                CreatedAt = DateTimeOffset.UtcNow,
                IsBlocked = false
            }
        };

        var sut = new BlockedItemsCalculator();

        var result = sut.Calculate(
            workItems,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        result.ItemsCount.Should().Be(1);
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