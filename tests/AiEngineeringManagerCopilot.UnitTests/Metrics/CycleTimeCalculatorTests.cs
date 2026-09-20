using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class CycleTimeCalculatorTests
{
    private readonly CycleTimeCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldReturnAverageCycleTime()
    {
        var periodStart = new DateOnly(2026, 9, 1);
        var periodEnd = new DateOnly(2026, 9, 30);

        var pullRequests =
            new List<PullRequest>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateTimeOffset(
                        2026, 9, 10, 8, 0, 0,
                        TimeSpan.Zero),
                    MergedAt = new DateTimeOffset(
                        2026, 9, 10, 12, 0, 0,
                        TimeSpan.Zero)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateTimeOffset(
                        2026, 9, 11, 8, 0, 0,
                        TimeSpan.Zero),
                    MergedAt = new DateTimeOffset(
                        2026, 9, 11, 18, 0, 0,
                        TimeSpan.Zero)
                }
            };

        var result = _calculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(7);
        result.PullRequestsCount.Should().Be(2);
    }

    [Fact]
    public void Calculate_ShouldIgnoreUnmergedPullRequests()
    {
        var periodStart = new DateOnly(2026, 9, 1);
        var periodEnd = new DateOnly(2026, 9, 30);

        var pullRequests =
            new List<PullRequest>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateTimeOffset(
                        2026, 9, 10, 8, 0, 0,
                        TimeSpan.Zero)
                }
            };

        var result = _calculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(0);
        result.PullRequestsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldIgnorePullRequestsOutsidePeriod()
    {
        var periodStart = new DateOnly(2026, 9, 1);
        var periodEnd = new DateOnly(2026, 9, 30);

        var pullRequests =
            new List<PullRequest>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateTimeOffset(
                        2026, 8, 31, 8, 0, 0,
                        TimeSpan.Zero),
                    MergedAt = new DateTimeOffset(
                        2026, 8, 31, 12, 0, 0,
                        TimeSpan.Zero)
                }
            };

        var result = _calculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(0);
        result.PullRequestsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldIgnoreInvalidNegativeDuration()
    {
        var periodStart = new DateOnly(2026, 9, 1);
        var periodEnd = new DateOnly(2026, 9, 30);

        var pullRequests =
            new List<PullRequest>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateTimeOffset(
                        2026, 9, 10, 18, 0, 0,
                        TimeSpan.Zero),
                    MergedAt = new DateTimeOffset(
                        2026, 9, 10, 12, 0, 0,
                        TimeSpan.Zero)
                }
            };

        var result = _calculator.Calculate(
            pullRequests,
            periodStart,
            periodEnd);

        result.AverageHours.Should().Be(0);
        result.PullRequestsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoPullRequestsExist()
    {
        var result = _calculator.Calculate(
            [],
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        result.AverageHours.Should().Be(0);
        result.PullRequestsCount.Should().Be(0);
    }
    
    [Fact]
    public void Calculate_ShouldUseMergedAtToDeterminePeriod()
    {
        var pullRequests = new[]
        {
            // Created before September, merged in September -> included
            new PullRequest
            {
                Id = Guid.NewGuid(),
                RepositoryId = Guid.NewGuid(),
                ExternalId = 1,
                AuthorExternalId = "user-1",
                Title = "PR merged in September",
                State = PullRequestState.Merged,
                CreatedAt = DateTimeOffset.Parse(
                    "2026-08-28T10:00:00Z"),
                MergedAt = DateTimeOffset.Parse(
                    "2026-09-03T10:00:00Z")
            },

            // Created in September, merged in October -> excluded
            new PullRequest
            {
                Id = Guid.NewGuid(),
                RepositoryId = Guid.NewGuid(),
                ExternalId = 2,
                AuthorExternalId = "user-2",
                Title = "PR merged in October",
                State = PullRequestState.Merged,
                CreatedAt = DateTimeOffset.Parse(
                    "2026-09-03T10:00:00Z"),
                MergedAt = DateTimeOffset.Parse(
                    "2026-10-01T10:00:00Z")
            }
        };

        var result = _calculator.Calculate(
            pullRequests,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        result.AverageHours.Should().Be(144);
    }
}