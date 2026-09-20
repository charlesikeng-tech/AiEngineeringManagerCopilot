using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IPullRequestRepository
{
    Task<IReadOnlyList<PullRequest>> GetMergedByTeamAndPeriodAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<PullRequest>> GetByTeamAndPeriodAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);

    Task<PullRequest?> GetByExternalIdAsync(
        Guid repositoryId,
        long externalId,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<PullRequest>>
        GetOpenByTeamAtDateAsync(
            Guid teamId,
            DateOnly date,
            CancellationToken cancellationToken);
    

    Task AddAsync(
        PullRequest pullRequest,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}