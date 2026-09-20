using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IJiraWorkItemRepository
{
    Task<JiraWorkItem?> GetByExternalIdAsync(
        Guid teamId,
        string externalId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraWorkItem>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task AddAsync(
        JiraWorkItem workItem,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<JiraWorkItem>> GetByTeamAndPeriodAsync(
        Guid teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);
}