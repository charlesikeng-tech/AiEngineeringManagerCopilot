using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class PRReviewTimeCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnAverageReviewTime()
    {
        var baseDate = new DateTimeOffset(
            2026, 1, 10, 10, 0, 0, TimeSpan.Zero);

        var pullRequest1 = CreatePullRequest(
            baseDate);

        var pullRequest2 = CreatePullRequest(
            baseDate.AddDays(1));

        var reviews = new[]
        {
            CreateReview(
                pullRequest1.Id,
                baseDate.AddHours(2)),

            CreateReview(
                pullRequest2.Id,
                baseDate.AddDays(1).AddHours(6))
        };

        var calculator = new PRReviewTimeCalculator();

        var result = calculator.Calculate(
            [pullRequest1, pullRequest2],
            reviews,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.AverageHours.Should().Be(4);
        result.PullRequestsCount.Should().Be(2);
    }

    [Fact]
    public void Calculate_ShouldUseFirstReview()
    {
        var createdAt = new DateTimeOffset(
            2026, 1, 10, 10, 0, 0, TimeSpan.Zero);

        var pullRequest = CreatePullRequest(createdAt);

        var reviews = new[]
        {
            CreateReview(
                pullRequest.Id,
                createdAt.AddHours(2)),

            CreateReview(
                pullRequest.Id,
                createdAt.AddHours(8))
        };

        var calculator = new PRReviewTimeCalculator();

        var result = calculator.Calculate(
            [pullRequest],
            reviews,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.AverageHours.Should().Be(2);
        result.PullRequestsCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_ShouldIgnorePullRequestsWithoutReview()
    {
        var createdAt = new DateTimeOffset(
            2026, 1, 10, 10, 0, 0, TimeSpan.Zero);

        var pullRequest = CreatePullRequest(createdAt);

        var calculator = new PRReviewTimeCalculator();

        var result = calculator.Calculate(
            [pullRequest],
            [],
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.AverageHours.Should().Be(0);
        result.PullRequestsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldIgnorePullRequestsOutsidePeriod()
    {
        var createdAt = new DateTimeOffset(
            2026, 2, 10, 10, 0, 0, TimeSpan.Zero);

        var pullRequest = CreatePullRequest(createdAt);

        var reviews = new[]
        {
            CreateReview(
                pullRequest.Id,
                createdAt.AddHours(2))
        };

        var calculator = new PRReviewTimeCalculator();

        var result = calculator.Calculate(
            [pullRequest],
            reviews,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.AverageHours.Should().Be(0);
        result.PullRequestsCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_ShouldIgnoreInvalidNegativeDuration()
    {
        var createdAt = new DateTimeOffset(
            2026, 1, 10, 10, 0, 0, TimeSpan.Zero);

        var pullRequest = CreatePullRequest(createdAt);

        var reviews = new[]
        {
            CreateReview(
                pullRequest.Id,
                createdAt.AddHours(-1))
        };

        var calculator = new PRReviewTimeCalculator();

        var result = calculator.Calculate(
            [pullRequest],
            reviews,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.AverageHours.Should().Be(0);
        result.PullRequestsCount.Should().Be(0);
    }
    
    [Fact]
    public void Calculate_ShouldUseMergedAtToDeterminePeriod()
    {
        var pullRequest = new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = Guid.NewGuid(),
            ExternalId = 1,
            AuthorExternalId = "test-user",
            Title = "Test PR",
            State = PullRequestState.Merged,
            CreatedAt = DateTimeOffset.Parse(
                "2026-08-30T10:00:00Z"),
            MergedAt = DateTimeOffset.Parse(
                "2026-09-05T10:00:00Z"),
            ClosedAt = DateTimeOffset.Parse(
                "2026-09-05T10:00:00Z")
        };

        var reviews = new[]
        {
            new PullRequestReview
            {
                Id = Guid.NewGuid(),
                ExternalId = 1,
                PullRequestId = pullRequest.Id,
                ReviewerExternalId = "reviewer",
                SubmittedAt = DateTimeOffset.Parse(
                    "2026-09-02T10:00:00Z"),
                State = PullRequestReviewState.Approved
            }
        };
        
        var calculator = new PRReviewTimeCalculator();

        var result = calculator.Calculate(
            new[] { pullRequest },
            reviews,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        result.AverageHours.Should().Be(72);
    }

    private static PullRequest CreatePullRequest(
        DateTimeOffset createdAt)
    {
        return new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = Guid.NewGuid(),
            ExternalId = Random.Shared.NextInt64(
                1,
                long.MaxValue),
            AuthorExternalId = "author",
            Title = "Test PR",
            State = PullRequestState.Merged,
            CreatedAt = createdAt,
            MergedAt = createdAt.AddHours(10)
        };
    }

    private static PullRequestReview CreateReview(
        Guid pullRequestId,
        DateTimeOffset submittedAt)
    {
        return new PullRequestReview
        {
            Id = Guid.NewGuid(),
            PullRequestId = pullRequestId,
            ReviewerExternalId = "reviewer",
            SubmittedAt = submittedAt,
            State = PullRequestReviewState.Approved
        };
    }
}