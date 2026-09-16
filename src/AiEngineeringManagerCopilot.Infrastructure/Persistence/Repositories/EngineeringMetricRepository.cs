using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class EngineeringMetricRepository(
    AppDbContext dbContext)
    : IEngineeringMetricRepository
{
    public async Task AddAsync(
        EngineeringMetric metric,
        CancellationToken cancellationToken)
    {
        await dbContext.EngineeringMetrics.AddAsync(
            metric,
            cancellationToken);
    }

    public async Task<IReadOnlyList<EngineeringMetric>>
        GetByTeamAndPeriodAsync(
            Guid teamId,
            MetricType metricType,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        return await dbContext.EngineeringMetrics
            .Where(x =>
                x.TeamId == teamId &&
                x.MetricType == metricType &&
                x.PeriodStart == periodStart &&
                x.PeriodEnd == periodEnd)
            .OrderBy(x => x.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}