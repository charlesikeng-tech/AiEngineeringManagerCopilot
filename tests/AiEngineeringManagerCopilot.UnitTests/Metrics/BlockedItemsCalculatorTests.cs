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
        var pullRequests = new List<PullRequest>
        {
            CreatePullRequest(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 1, 11, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ItemsCount.Should().Be(2);
    }

    [Fact]
    public void Calculate_ShouldIgnoreNonBlockedItems()
    {
        var pullRequests = new List<PullRequest>
        {
            CreatePullRequest(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                isBlocked: false,
                new DateTimeOffset(
                    2026, 1, 11, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ItemsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldIgnoreItemsOutsidePeriod()
    {
        var pullRequests = new List<PullRequest>
        {
            CreatePullRequest(
                isBlocked: true,
                new DateTimeOffset(
                    2025, 12, 31, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                isBlocked: true,
                new DateTimeOffset(
                    2026, 2, 1, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ItemsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoBlockedItemsExist()
    {
        var pullRequests = new List<PullRequest>
        {
            CreatePullRequest(
                isBlocked: false,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ItemsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenPeriodIsInvalid()
    {
        var pullRequests = new List<PullRequest>();

        var action = () => _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 1));

        action.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                "periodStart must be before or equal to periodEnd.");
    }

    private static PullRequest CreatePullRequest(
        bool isBlocked,
        DateTimeOffset createdAt)
    {
        return new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = Guid.NewGuid(),
            ExternalId = Random.Shared.NextInt64(1, long.MaxValue),
            AuthorExternalId = "test-user",
            Title = "Test PR",
            State = Domain.Enums.PullRequestState.Open,
            CreatedAt = createdAt,
            IsBlocked = isBlocked
        };
    }
}