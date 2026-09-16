using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IEngineeringMetricRepository
{
    Task AddAsync(
        EngineeringMetric metric,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringMetric>> GetByTeamAndPeriodAsync(
        Guid teamId,
        MetricType metricType,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}