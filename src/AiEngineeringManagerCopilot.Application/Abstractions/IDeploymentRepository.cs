using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IDeploymentRepository
{
    Task AddAsync(
        Deployment deployment,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Deployment>> GetByTeamAndPeriodAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}