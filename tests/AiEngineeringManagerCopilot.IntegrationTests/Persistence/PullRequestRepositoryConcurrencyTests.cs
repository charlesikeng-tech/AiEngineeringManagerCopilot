using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Persistence;

public sealed class PullRequestRepositoryConcurrencyTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PullRequestRepositoryConcurrencyTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpsertAsync_WhenCalledConcurrently_ShouldPersistSinglePullRequest()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        const long externalId = 987654321L;

        await CreateTeamAndRepositoryAsync(
            teamId,
            repositoryId);

        var createdAt =
            new DateTimeOffset(
                2026, 10, 1,
                10, 0, 0,
                TimeSpan.Zero);

        var first = new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            ExternalId = externalId,
            AuthorExternalId = "author-1",
            Title = "First title",
            State = PullRequestState.Open,
            CreatedAt = createdAt,
            IsBlocked = false
        };

        var second = new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            ExternalId = externalId,
            AuthorExternalId = "author-2",
            Title = "Second title",
            State = PullRequestState.Open,
            CreatedAt = createdAt,
            IsBlocked = false
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
            await dbContext.PullRequests
                .AsNoTracking()
                .Where(x =>
                    x.RepositoryId == repositoryId &&
                    x.ExternalId == externalId)
                .ToListAsync();

        persisted.Should().ContainSingle();

        var result = persisted.Single();

        var isFirst =
            result.Title == "First title" &&
            result.AuthorExternalId == "author-1";

        var isSecond =
            result.Title == "Second title" &&
            result.AuthorExternalId == "author-2";

        (isFirst || isSecond).Should().BeTrue();
    }

    [Fact]
    public async Task UpsertAsync_WhenPullRequestExists_ShouldPreserveIsBlocked()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        const long externalId = 123456L;

        await CreateTeamAndRepositoryAsync(
            teamId,
            repositoryId);

        var createdAt =
            DateTimeOffset.UtcNow.AddDays(-1);

        var initial = new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            ExternalId = externalId,
            AuthorExternalId = "author",
            Title = "Initial title",
            State = PullRequestState.Open,
            CreatedAt = createdAt,
            IsBlocked = true
        };

        await UpsertInSeparateScopeAsync(initial);

        var refreshedFromGitHub = new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            ExternalId = externalId,
            AuthorExternalId = "author",
            Title = "Updated title",
            State = PullRequestState.Open,
            CreatedAt = createdAt,
            IsBlocked = false
        };

        // Act
        await UpsertInSeparateScopeAsync(
            refreshedFromGitHub);

        // Assert
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var persisted =
            await dbContext.PullRequests
                .AsNoTracking()
                .SingleAsync(x =>
                    x.RepositoryId == repositoryId &&
                    x.ExternalId == externalId);

        persisted.Title.Should().Be("Updated title");
        persisted.IsBlocked.Should().BeTrue();
    }

    private async Task UpsertInSeparateScopeAsync(
        PullRequest pullRequest)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IPullRequestRepository>();

        await repository.UpsertAsync(
            pullRequest,
            CancellationToken.None);
    }

    private async Task CreateTeamAndRepositoryAsync(
        Guid teamId,
        Guid repositoryId)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var ownerUserId =
            Guid.Parse(
                "11111111-1111-1111-1111-111111111111");

        dbContext.Teams.Add(
            new Team
            {
                Id = teamId,
                Name = $"PR Concurrency Team {teamId:N}",
                OwnerUserId = ownerUserId
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

        await dbContext.SaveChangesAsync();
    }
}