using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.AI;

public interface IAIAnalysisActionRepository
{
    Task AddRangeAsync(
        IReadOnlyList<AIAnalysisAction> actions,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AIAnalysisAction>> GetByAnalysisIdAsync(
        Guid analysisId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}