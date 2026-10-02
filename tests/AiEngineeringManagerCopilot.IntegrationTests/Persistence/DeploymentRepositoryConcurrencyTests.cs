using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Persistence;

public sealed class DeploymentRepositoryConcurrencyTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DeploymentRepositoryConcurrencyTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpsertAsync_WhenCalledConcurrently_ShouldPersistSingleDeployment()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();

        const long externalId = 987654321L;

        await CreateTeamAndRepositoryAsync(
            teamId,
            repositoryId);

        var deployedAt =
            new DateTimeOffset(
                2026,
                10,
                2,
                12,
                0,
                0,
                TimeSpan.Zero);

        var first = new Deployment
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            ExternalId = externalId,
            Environment = "production",
            Status = "success",
            DeployedAt = deployedAt
        };

        var second = new Deployment
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            ExternalId = externalId,
            Environment = "staging",
            Status = "failure",
            DeployedAt = deployedAt.AddMinutes(5)
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
            await dbContext.Deployments
                .AsNoTracking()
                .Where(x =>
                    x.RepositoryId == repositoryId &&
                    x.ExternalId == externalId)
                .ToListAsync();

        persisted.Should().ContainSingle();

        var result = persisted.Single();

        var isFirst =
            result.Environment == "production" &&
            result.Status == "success" &&
            result.DeployedAt == deployedAt;

        var isSecond =
            result.Environment == "staging" &&
            result.Status == "failure" &&
            result.DeployedAt == deployedAt.AddMinutes(5);

        (isFirst || isSecond)
            .Should()
            .BeTrue();
    }

    private async Task UpsertInSeparateScopeAsync(
        Deployment deployment)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IDeploymentRepository>();

        await repository.UpsertAsync(
            deployment,
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
                Name = $"Deployment Concurrency Team {teamId:N}",
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