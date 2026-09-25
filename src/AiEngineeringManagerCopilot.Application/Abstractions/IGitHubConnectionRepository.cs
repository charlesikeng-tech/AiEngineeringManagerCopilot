using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IGitHubConnectionRepository
{
    Task<GitHubConnection?> GetByTeamIdAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task AddAsync(
        GitHubConnection connection,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        GitHubConnection connection,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<GitHubConnection>> GetAllAsync(
        CancellationToken cancellationToken);
}