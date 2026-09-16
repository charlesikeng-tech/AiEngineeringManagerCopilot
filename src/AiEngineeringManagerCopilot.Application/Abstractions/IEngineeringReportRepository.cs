using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IEngineeringReportRepository
{
    Task AddAsync(
        EngineeringReport report,
        CancellationToken cancellationToken);

    Task<EngineeringReport?> GetByIdAsync(
        Guid reportId,
        Guid teamId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringReport>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}