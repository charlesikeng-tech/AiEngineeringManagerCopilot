using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class TeamRepository(
    AppDbContext dbContext) : ITeamRepository
{
    public async Task<Team?> GetByIdAsync(
        Guid teamId,
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Teams
            .FirstOrDefaultAsync(
                x =>
                    x.Id == teamId &&
                    x.OwnerUserId == ownerUserId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Team>> GetByOwnerAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Teams
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Team team,
        CancellationToken cancellationToken)
    {
        await dbContext.Teams.AddAsync(
            team,
            cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(
        Team team,
        CancellationToken cancellationToken)
    {
        dbContext.Teams.Remove(team);

        return Task.CompletedTask;
    }
}