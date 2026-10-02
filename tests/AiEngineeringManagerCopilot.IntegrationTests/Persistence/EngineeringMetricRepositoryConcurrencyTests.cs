using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Persistence;

public sealed class EngineeringMetricRepositoryConcurrencyTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EngineeringMetricRepositoryConcurrencyTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpsertAsync_WhenSameMetricIsWrittenConcurrently_ShouldKeepSingleRow()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerUserId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        var periodStart = new DateOnly(2026, 9, 1);
        var periodEnd = new DateOnly(2026, 9, 30);

        await CreateTeamAsync(
            teamId,
            ownerUserId);

        var firstMetric = CreateMetric(
            teamId,
            periodStart,
            periodEnd,
            10m);

        var secondMetric = CreateMetric(
            teamId,
            periodStart,
            periodEnd,
            20m);

        // Act
        var firstTask = UpsertAsync(firstMetric);
        var secondTask = UpsertAsync(secondMetric);

        var act = async () =>
            await Task.WhenAll(
                firstTask,
                secondTask);

        // Assert
        await act.Should().NotThrowAsync();

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var persistedMetrics =
            await dbContext.EngineeringMetrics
                .AsNoTracking()
                .Where(x =>
                    x.TeamId == teamId &&
                    x.MetricType == MetricType.CycleTime &&
                    x.PeriodStart == periodStart &&
                    x.PeriodEnd == periodEnd)
                .ToListAsync();

        persistedMetrics.Should().ContainSingle();

        persistedMetrics[0].Value.Should()
            .BeOneOf(10m, 20m);
    }
    
    [Fact]
    public async Task UpsertAsync_WhenMetricAlreadyExists_ShouldPreserveCreatedAtAndUpdateUpdatedAt()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerUserId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        var periodStart = new DateOnly(2026, 9, 1);
        var periodEnd = new DateOnly(2026, 9, 30);

        await CreateTeamAsync(
            teamId,
            ownerUserId);

        var createdAt =
            new DateTimeOffset(
                2026, 9, 30,
                10, 0, 0,
                TimeSpan.Zero);

        var firstUpdatedAt = createdAt;

        var secondUpdatedAt =
            createdAt.AddHours(2);

        var firstMetric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.CycleTime,
            Value = 10m,
            DataStatus = MetricDataStatus.Available,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = createdAt,
            UpdatedAt = firstUpdatedAt
        };

        EngineeringMetric firstPersisted;

        await using (var scope =
                     _factory.Services.CreateAsyncScope())
        {
            var repository =
                scope.ServiceProvider
                    .GetRequiredService<IEngineeringMetricRepository>();

            firstPersisted =
                await repository.UpsertAsync(
                    firstMetric,
                    CancellationToken.None);
        }

        var secondMetric = new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.CycleTime,
            Value = 20m,
            DataStatus = MetricDataStatus.Available,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = secondUpdatedAt,
            UpdatedAt = secondUpdatedAt
        };

        // Act
        EngineeringMetric secondPersisted;

        await using (var scope =
                     _factory.Services.CreateAsyncScope())
        {
            var repository =
                scope.ServiceProvider
                    .GetRequiredService<IEngineeringMetricRepository>();

            secondPersisted =
                await repository.UpsertAsync(
                    secondMetric,
                    CancellationToken.None);
        }

        // Assert
        secondPersisted.Id.Should()
            .Be(firstPersisted.Id);

        secondPersisted.Value.Should()
            .Be(20m);

        secondPersisted.CreatedAt.Should()
            .Be(createdAt);

        secondPersisted.UpdatedAt.Should()
            .Be(secondUpdatedAt);
    }
    
    [Fact]
    public async Task UpsertAsync_WhenCalledConcurrently_ShouldPersistSingleRepository()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var externalId = 123456789L;

        await CreateTeamAsync(
            teamId,
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var firstRepository = new Repository
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ExternalId = externalId,
            Name = "repository-first",
            FullName = "km/repository-first",
            Url = "https://example.test/repository-first",
            DefaultBranch = "main",
            IsActive = true
        };

        var secondRepository = new Repository
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ExternalId = externalId,
            Name = "repository-second",
            FullName = "km/repository-second",
            Url = "https://example.test/repository-second",
            DefaultBranch = "main",
            IsActive = true
        };

        // Act
        var act = async () =>
        {
            await Task.WhenAll(
                UpsertInSeparateScopeAsync(firstRepository),
                UpsertInSeparateScopeAsync(secondRepository));
        };

        // Assert
        await act.Should().NotThrowAsync();

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var persisted =
            await dbContext.Repositories
                .AsNoTracking()
                .Where(x =>
                    x.TeamId == teamId &&
                    x.ExternalId == externalId)
                .ToListAsync();

        persisted.Should().ContainSingle();

        var persistedRepository = persisted.Single();

        var isFirst =
            persistedRepository.Name == "repository-first" &&
            persistedRepository.FullName == "km/repository-first";

        var isSecond =
            persistedRepository.Name == "repository-second" &&
            persistedRepository.FullName == "km/repository-second";

        (isFirst || isSecond).Should().BeTrue();
    }

    private async Task UpsertAsync(
        EngineeringMetric metric)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IEngineeringMetricRepository>();

        await repository.UpsertAsync(
            metric,
            CancellationToken.None);
    }

    private async Task CreateTeamAsync(
        Guid teamId,
        Guid ownerUserId)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        dbContext.Teams.Add(
            new Team
            {
                Id = teamId,
                OwnerUserId = ownerUserId,
                Name = $"Concurrency Test {teamId:N}",
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static EngineeringMetric CreateMetric(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal value)
    {
        return new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MetricType = MetricType.CycleTime,
            Value = value,
            DataStatus = MetricDataStatus.Available,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    
    private async Task UpsertInSeparateScopeAsync(
        Repository repository)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var repositoryRepository =
            scope.ServiceProvider
                .GetRequiredService<IRepositoryRepository>();

        await repositoryRepository.UpsertAsync(
            repository,
            CancellationToken.None);
    }
}