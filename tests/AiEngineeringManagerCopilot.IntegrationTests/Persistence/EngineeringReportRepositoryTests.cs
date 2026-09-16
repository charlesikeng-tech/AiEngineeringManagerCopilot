using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Persistence;

public sealed class EngineeringReportRepositoryTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EngineeringReportRepositoryTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }
    
    private static async Task<Guid> CreateTeamAsync(
        AppDbContext dbContext)
    {
        var teamId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            OwnerUserId = Guid.Parse(
                "11111111-1111-1111-1111-111111111111"),
            Name = $"Test Team {Guid.NewGuid():N}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.Teams.AddAsync(team);

        await dbContext.SaveChangesAsync();

        return teamId;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistReport()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository =
            new EngineeringReportRepository(dbContext);
        
        var teamId = await CreateTeamAsync(dbContext);

        var report = new EngineeringReport
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            PeriodStart = new DateOnly(2026, 9, 1),
            PeriodEnd = new DateOnly(2026, 9, 30),
            ExecutiveSummary = "Team health is good.",
            OverallScore = 85,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await repository.AddAsync(
            report,
            CancellationToken.None);

        await repository.SaveChangesAsync(
            CancellationToken.None);

        var persisted =
            await dbContext.EngineeringReports
                .FirstOrDefaultAsync(x => x.Id == report.Id);

        persisted.Should().NotBeNull();
        persisted!.TeamId.Should().Be(teamId);
        persisted.OverallScore.Should().Be(85);
        persisted.ExecutiveSummary
            .Should()
            .Be("Team health is good.");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnReportForTeam()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository =
            new EngineeringReportRepository(dbContext);

        var teamId = await CreateTeamAsync(dbContext);

        var report = new EngineeringReport
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            PeriodStart = new DateOnly(2026, 9, 1),
            PeriodEnd = new DateOnly(2026, 9, 30),
            ExecutiveSummary = "Healthy team.",
            OverallScore = 90,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await repository.AddAsync(
            report,
            CancellationToken.None);

        await repository.SaveChangesAsync(
            CancellationToken.None);

        var result =
            await repository.GetByIdAsync(
                report.Id,
                teamId,
                CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(report.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForAnotherTeam()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository =
            new EngineeringReportRepository(dbContext);

        var teamId = await CreateTeamAsync(dbContext);
        var anotherTeamId = await CreateTeamAsync(dbContext);

        var report = new EngineeringReport
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            PeriodStart = new DateOnly(2026, 9, 1),
            PeriodEnd = new DateOnly(2026, 9, 30),
            ExecutiveSummary = "Private report.",
            OverallScore = 70,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await repository.AddAsync(
            report,
            CancellationToken.None);

        await repository.SaveChangesAsync(
            CancellationToken.None);

        var result =
            await repository.GetByIdAsync(
                report.Id,
                anotherTeamId,
                CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTeamAsync_ShouldReturnReportsOrderedByCreationDate()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository =
            new EngineeringReportRepository(dbContext);

        var teamId = await CreateTeamAsync(dbContext);

        var olderReport = new EngineeringReport
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            PeriodStart = new DateOnly(2026, 8, 1),
            PeriodEnd = new DateOnly(2026, 8, 31),
            ExecutiveSummary = "August report.",
            OverallScore = 75,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var newerReport = new EngineeringReport
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            PeriodStart = new DateOnly(2026, 9, 1),
            PeriodEnd = new DateOnly(2026, 9, 30),
            ExecutiveSummary = "September report.",
            OverallScore = 90,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await repository.AddAsync(
            olderReport,
            CancellationToken.None);

        await repository.AddAsync(
            newerReport,
            CancellationToken.None);

        await repository.SaveChangesAsync(
            CancellationToken.None);

        var results =
            await repository.GetByTeamAsync(
                teamId,
                CancellationToken.None);

        results.Should().HaveCount(2);
        results[0].Id.Should().Be(newerReport.Id);
        results[1].Id.Should().Be(olderReport.Id);
    }
    
}