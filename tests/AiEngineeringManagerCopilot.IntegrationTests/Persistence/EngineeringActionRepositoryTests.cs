using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Persistence;

public sealed class EngineeringActionRepositoryTests(
    CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task AddRangeAsync_ShouldPersistActions()
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var reportId = Guid.NewGuid();
        await SeedReportAsync(dbContext, reportId);

        var actions = new[]
        {
            new EngineeringAction
            {
                Id = Guid.NewGuid(),
                ReportId = reportId,
                Title = "Improve PR review time",
                Description = "Reduce average PR review time.",
                Owner = "Engineering Manager",
                DueDate = new DateOnly(2026, 10, 1),
                Status = ActionStatus.Todo,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new EngineeringAction
            {
                Id = Guid.NewGuid(),
                ReportId = reportId,
                Title = "Reduce blocked items",
                Description = "Identify and remove recurring blockers.",
                Owner = "Tech Lead",
                DueDate = new DateOnly(2026, 10, 15),
                Status = ActionStatus.InProgress,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        dbContext.EngineeringActions.AddRange(actions);

        await dbContext.SaveChangesAsync();

        var persisted = await dbContext.EngineeringActions
            .Where(x => x.ReportId == reportId)
            .ToListAsync();

        persisted.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByReportId_ShouldReturnOnlyReportActions()
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var reportId = Guid.NewGuid();
        
        var otherReportId = Guid.NewGuid();
        await SeedReportAsync(dbContext, reportId);
        await SeedReportAsync(dbContext, otherReportId);

        dbContext.EngineeringActions.AddRange(
            new EngineeringAction
            {
                Id = Guid.NewGuid(),
                ReportId = reportId,
                Title = "Action 1",
                Description = "Description 1",
                Status = ActionStatus.Todo,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new EngineeringAction
            {
                Id = Guid.NewGuid(),
                ReportId = otherReportId,
                Title = "Action 2",
                Description = "Description 2",
                Status = ActionStatus.Todo,
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var actions = await dbContext.EngineeringActions
            .Where(x => x.ReportId == reportId)
            .ToListAsync();

        actions.Should().ContainSingle();
        actions[0].Title.Should().Be("Action 1");
    }

    [Fact]
    public async Task GetByReportId_ShouldOrderByStatusThenDueDateThenTitle()
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var reportId = Guid.NewGuid();
        await SeedReportAsync(dbContext, reportId);
        
        dbContext.EngineeringActions.AddRange(
            new EngineeringAction
            {
                Id = Guid.NewGuid(),
                ReportId = reportId,
                Title = "Done action",
                Description = "Done",
                Status = ActionStatus.Done,
                DueDate = new DateOnly(2026, 10, 1),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new EngineeringAction
            {
                Id = Guid.NewGuid(),
                ReportId = reportId,
                Title = "Todo later",
                Description = "Todo later",
                Status = ActionStatus.Todo,
                DueDate = new DateOnly(2026, 11, 1),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new EngineeringAction
            {
                Id = Guid.NewGuid(),
                ReportId = reportId,
                Title = "Todo sooner",
                Description = "Todo sooner",
                Status = ActionStatus.Todo,
                DueDate = new DateOnly(2026, 10, 1),
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var repository = scope.ServiceProvider
            .GetRequiredService<IEngineeringActionRepository>();

        var actions = await repository.GetByReportIdAsync(
            reportId,
            CancellationToken.None);

        actions.Should().HaveCount(3);
        actions[0].Title.Should().Be("Todo sooner");
        actions[1].Title.Should().Be("Todo later");
        actions[2].Title.Should().Be("Done action");
    }
    
    private static async Task SeedReportAsync(
        AppDbContext dbContext,
        Guid reportId)
    {
        var teamId = Guid.NewGuid();

        dbContext.Teams.Add(
            new Team
            {
                Id = teamId,
                OwnerUserId = Guid.Parse(
                    "11111111-1111-1111-1111-111111111111"),
                Name = "Test Team",
                Description = "Test team",
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();

        dbContext.EngineeringReports.Add(
            new EngineeringReport
            {
                Id = reportId,
                TeamId = teamId,
                PeriodStart = new DateOnly(2026, 9, 1),
                PeriodEnd = new DateOnly(2026, 9, 30),
                ExecutiveSummary = "Test report",
                OverallScore = 80,
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }
}