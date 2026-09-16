using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
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

        var pullRequests = new[]
        {
            CreatePullRequest(
                new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 10, 18, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                new DateTimeOffset(2026, 1, 11, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 12, 4, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                new DateTimeOffset(2026, 1, 13, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 13, 23, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(15);
        result.PullRequestsCount.Should().Be(3);
    }

    [Fact]
    public void Calculate_ShouldIgnoreUnmergedPullRequests()
    {
        var periodStart = new DateOnly(2026, 1, 1);
        var periodEnd = new DateOnly(2026, 1, 31);

        var pullRequests = new[]
        {
            CreatePullRequest(
                new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
                null),

            CreatePullRequest(
                new DateTimeOffset(2026, 1, 11, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 11, 18, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(10);
        result.PullRequestsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldIgnorePullRequestsOutsidePeriod()
    {
        var periodStart = new DateOnly(2026, 1, 1);
        var periodEnd = new DateOnly(2026, 1, 31);

        var pullRequests = new[]
        {
            CreatePullRequest(
                new DateTimeOffset(2025, 12, 31, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 12, 31, 18, 0, 0, TimeSpan.Zero)),

            CreatePullRequest(
                new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 10, 18, 0, 0, TimeSpan.Zero))
        };

        var result = _calculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(10);
        result.PullRequestsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoEligiblePullRequestsExist()
    {
        var periodStart = new DateOnly(2026, 1, 1);
        var periodEnd = new DateOnly(2026, 1, 31);

        var pullRequests = new[]
        {
            CreatePullRequest(
                new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
                null)
        };

        var result = _calculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(0);
        result.PullRequestsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenPeriodIsInvalid()
    {
        var periodStart = new DateOnly(2026, 2, 1);
        var periodEnd = new DateOnly(2026, 1, 1);

        var act = () => _calculator.Calculate(
            Array.Empty<PullRequest>(),
            periodStart,
            periodEnd);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                "periodStart must be before or equal to periodEnd.");
    }

    private static PullRequest CreatePullRequest(
        DateTimeOffset createdAt,
        DateTimeOffset? mergedAt)
    {
        return new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = Guid.NewGuid(),
            ExternalId = Random.Shared.NextInt64(1, 1_000_000),
            AuthorExternalId = "author-1",
            Title = "Test PR",
            CreatedAt = createdAt,
            MergedAt = mergedAt,
            State = mergedAt.HasValue
                ? PullRequestState.Merged
                : PullRequestState.Open
        };
    }
}