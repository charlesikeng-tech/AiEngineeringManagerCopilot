using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    private static readonly Guid DevelopmentUserId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    private static readonly Guid DevelopmentTeamId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await SeedUserAsync(
            dbContext,
            cancellationToken);

        await SeedTeamAsync(
            dbContext,
            cancellationToken);
        
        await SeedEngineeringMetricsAsync(
            dbContext,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static async Task SeedUserAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users
            .AnyAsync(
                x => x.Id == DevelopmentUserId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.Users.Add(
            new User
            {
                Id = DevelopmentUserId,
                Email = "dev@ai-engineering-manager.local",
                Name = "Development User",
                CreatedAt = DateTimeOffset.UtcNow
            });
    }

    private static async Task SeedTeamAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Teams
            .AnyAsync(
                x => x.Id == DevelopmentTeamId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.Teams.Add(
            new Team
            {
                Id = DevelopmentTeamId,
                OwnerUserId = DevelopmentUserId,
                Name = "Development Team",
                Description = "Development environment team",
                CreatedAt = DateTimeOffset.UtcNow
            });
    }
    
    private static async Task SeedEngineeringMetricsAsync(
    AppDbContext dbContext,
    CancellationToken cancellationToken)
    {
        var hasMetrics = await dbContext.EngineeringMetrics
            .AnyAsync(
                x => x.TeamId == DevelopmentTeamId,
                cancellationToken);

        if (hasMetrics)
        {
            return;
        }

        var augustStart = new DateOnly(2026, 8, 1);
        var augustEnd = new DateOnly(2026, 8, 31);

        var septemberStart = new DateOnly(2026, 9, 1);
        var septemberEnd = new DateOnly(2026, 9, 30);

        var metrics = new[]
        {
            CreateMetric(
                MetricType.CycleTime,
                38m,
                augustStart,
                augustEnd),

            CreateMetric(
                MetricType.PRReviewTime,
                18m,
                augustStart,
                augustEnd),

            CreateMetric(
                MetricType.DeploymentFrequency,
                12m,
                augustStart,
                augustEnd),

            CreateMetric(
                MetricType.ChangeFailureRate,
                14m,
                augustStart,
                augustEnd),

            CreateMetric(
                MetricType.LeadTime,
                52m,
                augustStart,
                augustEnd),

            CreateMetric(
                MetricType.OpenPRs,
                14m,
                augustStart,
                augustEnd),

            CreateMetric(
                MetricType.MergedPRs,
                32m,
                augustStart,
                augustEnd),

            CreateMetric(
                MetricType.BlockedItems,
                5m,
                augustStart,
                augustEnd),

            CreateMetric(
                MetricType.CycleTime,
                56m,
                septemberStart,
                septemberEnd),

            CreateMetric(
                MetricType.PRReviewTime,
                11m,
                septemberStart,
                septemberEnd),

            CreateMetric(
                MetricType.DeploymentFrequency,
                18m,
                septemberStart,
                septemberEnd),

            CreateMetric(
                MetricType.ChangeFailureRate,
                24m,
                septemberStart,
                septemberEnd),

            CreateMetric(
                MetricType.LeadTime,
                36m,
                septemberStart,
                septemberEnd),

            CreateMetric(
                MetricType.OpenPRs,
                14m,
                septemberStart,
                septemberEnd),

            CreateMetric(
                MetricType.MergedPRs,
                47m,
                septemberStart,
                septemberEnd),

            CreateMetric(
                MetricType.BlockedItems,
                7m,
                septemberStart,
                septemberEnd)
        };

        dbContext.EngineeringMetrics.AddRange(metrics);
    }
    
    private static EngineeringMetric CreateMetric(
        MetricType metricType,
        decimal value,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        return new EngineeringMetric
        {
            Id = Guid.NewGuid(),
            TeamId = DevelopmentTeamId,
            MetricType = metricType,
            Value = value,
            DataStatus = MetricDataStatus.Available,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}