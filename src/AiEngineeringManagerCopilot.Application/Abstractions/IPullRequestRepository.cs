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
}