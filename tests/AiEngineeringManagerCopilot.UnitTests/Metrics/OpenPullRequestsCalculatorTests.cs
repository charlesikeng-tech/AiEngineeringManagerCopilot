using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class OpenPullRequestsCalculatorTests
{
    private readonly OpenPullRequestsCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldCountOpenPullRequests()
    {
        var pullRequests = new[]
        {
            CreatePullRequest(
                PullRequestState.Open,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Open,
                new DateTimeOffset(
                    2026, 1, 15, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Merged,
                new DateTimeOffset(
                    2026, 1, 20, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.PullRequestsCount.Should().Be(2);
    }

    [Fact]
    public void Calculate_ShouldIgnoreClosedAndMergedPullRequests()
    {
        var pullRequests = new[]
        {
            CreatePullRequest(
                PullRequestState.Open,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Merged,
                new DateTimeOffset(
                    2026, 1, 11, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Closed,
                new DateTimeOffset(
                    2026, 1, 12, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.PullRequestsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldIgnorePullRequestsOutsidePeriod()
    {
        var pullRequests = new[]
        {
            CreatePullRequest(
                PullRequestState.Open,
                new DateTimeOffset(
                    2025, 12, 31, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Open,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Open,
                new DateTimeOffset(
                    2026, 2, 1, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.PullRequestsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoOpenPullRequestsExist()
    {
        var pullRequests = new[]
        {
            CreatePullRequest(
                PullRequestState.Merged,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.PullRequestsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenPeriodIsInvalid()
    {
        var act = () => _calculator.Calculate(
            Array.Empty<PullRequest>(),
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 1));

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                "periodStart must be before or equal to periodEnd.");
    }

    private static PullRequest CreatePullRequest(
        PullRequestState state,
        DateTimeOffset createdAt)
    {
        return new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = Guid.NewGuid(),
            ExternalId = Random.Shared.NextInt64(
                1,
                1_000_000),
            AuthorExternalId = "test-user",
            Title = "Test PR",
            State = state,
            CreatedAt = createdAt,
            MergedAt = state == PullRequestState.Merged
                ? createdAt.AddHours(10)
                : null,
            ClosedAt = state == PullRequestState.Closed
                ? createdAt.AddHours(10)
                : null
        };
    }
}