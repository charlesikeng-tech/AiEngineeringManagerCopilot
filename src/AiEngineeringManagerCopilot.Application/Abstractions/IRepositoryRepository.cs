using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IRepositoryRepository
{
    Task<Repository?> GetByExternalIdAsync(
        Guid teamId,
        long externalId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Repository repository,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}