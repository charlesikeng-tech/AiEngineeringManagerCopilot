using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class JiraConnectionRepository(
    AppDbContext dbContext)
    : IJiraConnectionRepository
{
    public async Task<JiraConnection?> GetByTeamIdAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        return await dbContext.JiraConnections
            .FirstOrDefaultAsync(
                x => x.TeamId == teamId,
                cancellationToken);
    }

    public async Task AddAsync(
        JiraConnection connection,
        CancellationToken cancellationToken)
    {
        await dbContext.JiraConnections.AddAsync(
            connection,
            cancellationToken);
    }

    public Task DeleteAsync(
        JiraConnection connection,
        CancellationToken cancellationToken)
    {
        dbContext.JiraConnections.Remove(connection);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JiraConnection>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.JiraConnections
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}