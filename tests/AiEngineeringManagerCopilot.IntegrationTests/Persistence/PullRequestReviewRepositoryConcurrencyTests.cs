using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Persistence;

public sealed class PullRequestReviewRepositoryConcurrencyTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PullRequestReviewRepositoryConcurrencyTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpsertAsync_WhenCalledConcurrently_ShouldPersistSingleReview()
    {
        // Arrange
        var pullRequestId = Guid.NewGuid();
        const long externalId = 987654321L;

        await CreatePullRequestAsync(pullRequestId);

        var submittedAt =
            new DateTimeOffset(
                2026, 10, 2,
                10, 0, 0,
                TimeSpan.Zero);

        var first = new PullRequestReview
        {
            Id = Guid.NewGuid(),
            PullRequestId = pullRequestId,
            ExternalId = externalId,
            ReviewerExternalId = "reviewer-1",
            SubmittedAt = submittedAt,
            State = PullRequestReviewState.Approved
        };

        var second = new PullRequestReview
        {
            Id = Guid.NewGuid(),
            PullRequestId = pullRequestId,
            ExternalId = externalId,
            ReviewerExternalId = "reviewer-2",
            SubmittedAt = submittedAt.AddMinutes(5),
            State = PullRequestReviewState.ChangesRequested
        };

        // Act
        var act = async () =>
            await Task.WhenAll(
                UpsertInSeparateScopeAsync(first),
                UpsertInSeparateScopeAsync(second));

        // Assert
        await act.Should().NotThrowAsync();

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var persisted =
            await dbContext.PullRequestReviews
                .AsNoTracking()
                .Where(x =>
                    x.PullRequestId == pullRequestId &&
                    x.ExternalId == externalId)
                .ToListAsync();

        persisted.Should().ContainSingle();

        var result = persisted.Single();

        var isFirst =
            result.ReviewerExternalId == "reviewer-1" &&
            result.SubmittedAt == submittedAt &&
            result.State == PullRequestReviewState.Approved;

        var isSecond =
            result.ReviewerExternalId == "reviewer-2" &&
            result.SubmittedAt == submittedAt.AddMinutes(5) &&
            result.State == PullRequestReviewState.ChangesRequested;

        (isFirst || isSecond).Should().BeTrue();
    }

    private async Task UpsertInSeparateScopeAsync(
        PullRequestReview review)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IPullRequestReviewRepository>();

        await repository.UpsertAsync(
            review,
            CancellationToken.None);
    }

    private async Task CreatePullRequestAsync(
        Guid pullRequestId)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var teamId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();

        dbContext.Teams.Add(
            new Team
            {
                Id = teamId,
                Name = $"Review Concurrency Team {teamId:N}",
                OwnerUserId = Guid.Parse(
                    "11111111-1111-1111-1111-111111111111")
            });

        dbContext.Repositories.Add(
            new Repository
            {
                Id = repositoryId,
                TeamId = teamId,
                ExternalId = Random.Shared.NextInt64(
                    1,
                    long.MaxValue),
                Name = $"repository-{repositoryId:N}",
                FullName = $"km/repository-{repositoryId:N}",
                Url = $"https://example.test/{repositoryId:N}",
                DefaultBranch = "main",
                IsActive = true
            });

        dbContext.PullRequests.Add(
            new PullRequest
            {
                Id = pullRequestId,
                RepositoryId = repositoryId,
                ExternalId = Random.Shared.NextInt64(
                    1,
                    long.MaxValue),
                AuthorExternalId = "github-author",
                Title = "Pull request for review concurrency test",
                State = PullRequestState.Open,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                IsBlocked = false
            });

        await dbContext.SaveChangesAsync();
    }
}