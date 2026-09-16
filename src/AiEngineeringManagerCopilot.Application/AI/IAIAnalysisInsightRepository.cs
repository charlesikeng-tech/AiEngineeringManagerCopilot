using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.AI;

public interface IAIAnalysisInsightRepository
{
    Task AddRangeAsync(
        IReadOnlyList<AIAnalysisInsight> insights,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AIAnalysisInsight>> GetByAnalysisIdAsync(
        Guid analysisId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}