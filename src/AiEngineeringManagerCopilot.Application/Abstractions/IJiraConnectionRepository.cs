using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IJiraConnectionRepository
{
    Task<JiraConnection?> GetByTeamIdAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task AddAsync(
        JiraConnection connection,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        JiraConnection connection,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}