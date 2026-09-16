using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(
        Guid teamId,
        Guid ownerUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Team>> GetByOwnerAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Team team,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Team team,
        CancellationToken cancellationToken);
}