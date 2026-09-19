using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class RepositoryRepository(
    AppDbContext dbContext)
    : IRepositoryRepository
{
    public async Task<Repository?> GetByExternalIdAsync(
        Guid teamId,
        long externalId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Repositories
            .FirstOrDefaultAsync(
                x =>
                    x.TeamId == teamId &&
                    x.ExternalId == externalId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Repository>> GetByTeamIdAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Repositories
            .Where(x => x.TeamId == teamId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Repository repository,
        CancellationToken cancellationToken)
    {
        await dbContext.Repositories.AddAsync(
            repository,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}