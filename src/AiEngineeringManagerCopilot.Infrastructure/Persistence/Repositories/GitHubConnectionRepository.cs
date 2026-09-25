using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class GitHubConnectionRepository(
    AppDbContext dbContext)
    : IGitHubConnectionRepository
{
    public async Task<GitHubConnection?> GetByTeamIdAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        return await dbContext.GitHubConnections
            .FirstOrDefaultAsync(
                x => x.TeamId == teamId,
                cancellationToken);
    }

    public async Task AddAsync(
        GitHubConnection connection,
        CancellationToken cancellationToken)
    {
        await dbContext.GitHubConnections.AddAsync(
            connection,
            cancellationToken);
    }

    public Task DeleteAsync(
        GitHubConnection connection,
        CancellationToken cancellationToken)
    {
        dbContext.GitHubConnections.Remove(connection);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GitHubConnection>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.GitHubConnections
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}