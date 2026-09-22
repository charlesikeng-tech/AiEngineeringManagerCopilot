using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Persistence;

public sealed class EngineeringReportInsightRepositoryTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EngineeringReportInsightRepositoryTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddRangeAsync_ShouldPersistInsights()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var teamId = await CreateTeamAsync(dbContext);

        var report = new EngineeringReport
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            PeriodStart = new DateOnly(2026, 9, 1),
            PeriodEnd = new DateOnly(2026, 9, 30),
            ExecutiveSummary = "Test report",
            OverallScore = 60,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.EngineeringReports.AddAsync(report);
        await dbContext.SaveChangesAsync();

        var insights = new[]
        {
            new EngineeringReportInsight
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                Category = RiskCategory.Delivery,
                Title = "Cycle time is too high",
                Description = "Cycle time is above the target.",
                Impact = "Delivery is slower.",
                Recommendation = "Reduce pull request size."
            },
            new EngineeringReportInsight
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                Category = RiskCategory.Review,
                Title = "Review time is high",
                Description = "Review time is above the target.",
                Impact = "Reviews create bottlenecks.",
                Recommendation = "Define a review-time target."
            }
        };

        await dbContext.EngineeringReportInsights.AddRangeAsync(
            insights);

        await dbContext.SaveChangesAsync();

        var persisted =
            await dbContext.EngineeringReportInsights
                .Where(x => x.ReportId == report.Id)
                .ToListAsync();

        persisted.Should().HaveCount(2);
        persisted.Should().Contain(x =>
            x.Title == "Cycle time is too high");
        persisted.Should().Contain(x =>
            x.Title == "Review time is high");
    }

    [Fact]
    public async Task GetByReportId_ShouldReturnOnlyReportInsights()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var teamId = await CreateTeamAsync(dbContext);

        var report1 = await CreateReportAsync(
            dbContext,
            teamId);

        var report2 = await CreateReportAsync(
            dbContext,
            teamId);

        await dbContext.EngineeringReportInsights.AddRangeAsync(
            new EngineeringReportInsight
            {
                Id = Guid.NewGuid(),
                ReportId = report1.Id,
                Category = RiskCategory.Delivery,
                Title = "Report 1 insight",
                Description = "Description",
                Impact = "Impact",
                Recommendation = "Recommendation"
            },
            new EngineeringReportInsight
            {
                Id = Guid.NewGuid(),
                ReportId = report2.Id,
                Category = RiskCategory.Quality,
                Title = "Report 2 insight",
                Description = "Description",
                Impact = "Impact",
                Recommendation = "Recommendation"
            });

        await dbContext.SaveChangesAsync();

        var result =
            await dbContext.EngineeringReportInsights
                .Where(x => x.ReportId == report1.Id)
                .ToListAsync();

        result.Should().ContainSingle();
        result[0].Title.Should().Be("Report 1 insight");
    }

    [Fact]
    public async Task GetByReportId_ShouldOrderByCategoryThenTitle()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var teamId = await CreateTeamAsync(dbContext);

        var report = await CreateReportAsync(
            dbContext,
            teamId);

        await dbContext.EngineeringReportInsights.AddRangeAsync(
            new EngineeringReportInsight
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                Category = RiskCategory.Review,
                Title = "B insight",
                Description = "Description",
                Impact = "Impact",
                Recommendation = "Recommendation"
            },
            new EngineeringReportInsight
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                Category = RiskCategory.Delivery,
                Title = "Z insight",
                Description = "Description",
                Impact = "Impact",
                Recommendation = "Recommendation"
            },
            new EngineeringReportInsight
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                Category = RiskCategory.Delivery,
                Title = "A insight",
                Description = "Description",
                Impact = "Impact",
                Recommendation = "Recommendation"
            });

        await dbContext.SaveChangesAsync();

        var result =
            await dbContext.EngineeringReportInsights
                .Where(x => x.ReportId == report.Id)
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Title)
                .ToListAsync();

        result.Should().HaveCount(3);

        result[0].Title.Should().Be("A insight");
        result[1].Title.Should().Be("Z insight");
        result[2].Title.Should().Be("B insight");
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

    private static async Task<EngineeringReport> CreateReportAsync(
        AppDbContext dbContext,
        Guid teamId)
    {
        var report = new EngineeringReport
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            PeriodStart = new DateOnly(2026, 9, 1),
            PeriodEnd = new DateOnly(2026, 9, 30),
            ExecutiveSummary = "Test report",
            OverallScore = 75,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.EngineeringReports.AddAsync(report);
        await dbContext.SaveChangesAsync();

        return report;
    }
}