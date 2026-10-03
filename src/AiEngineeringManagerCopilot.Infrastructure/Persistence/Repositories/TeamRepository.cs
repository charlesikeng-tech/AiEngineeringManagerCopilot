using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
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

    public async Task<PagedResult<Team>> GetPageByOwnerAsync(
        Guid ownerUserId,
        int pageNumber,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Teams.AsNoTracking()
            .Where(team => team.OwnerUserId == ownerUserId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(team => team.Name.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(team => team.Name)
            .ThenBy(team => team.Id)
            .Skip(checked((pageNumber - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Team>(items, totalCount, pageNumber, pageSize);
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