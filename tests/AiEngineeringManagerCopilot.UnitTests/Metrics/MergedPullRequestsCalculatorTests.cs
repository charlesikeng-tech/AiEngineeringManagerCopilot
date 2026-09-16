using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class MergedPullRequestsCalculatorTests
{
    private readonly MergedPullRequestsCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldCountMergedPullRequests()
    {
        var pullRequests = new List<PullRequest>
        {
            CreatePullRequest(
                PullRequestState.Merged,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Merged,
                new DateTimeOffset(
                    2026, 1, 11, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.PullRequestsCount.Should().Be(2);
    }

    [Fact]
    public void Calculate_ShouldIgnoreOpenPullRequests()
    {
        var pullRequests = new List<PullRequest>
        {
            CreatePullRequest(
                PullRequestState.Merged,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Open,
                new DateTimeOffset(
                    2026, 1, 11, 10, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.PullRequestsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldIgnoreClosedPullRequests()
    {
        var pullRequests = new List<PullRequest>
        {
            CreatePullRequest(
                PullRequestState.Merged,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Closed,
                new DateTimeOffset(
                    2026, 1, 11, 10, 0, 0, TimeSpan.Zero))
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
        var pullRequests = new List<PullRequest>
        {
            CreatePullRequest(
                PullRequestState.Merged,
                new DateTimeOffset(
                    2025, 12, 31, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Merged,
                new DateTimeOffset(
                    2026, 1, 10, 10, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                PullRequestState.Merged,
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
        PullRequestState state,
        DateTimeOffset createdAt)
    {
        return new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = Guid.NewGuid(),
            ExternalId = Random.Shared.NextInt64(1, long.MaxValue),
            AuthorExternalId = "test-user",
            Title = "Test PR",
            State = state,
            CreatedAt = createdAt,
            MergedAt = state == PullRequestState.Merged
                ? createdAt.AddHours(10)
                : null,
            ClosedAt = state != PullRequestState.Open
                ? createdAt.AddHours(10)
                : null
        };
    }
}