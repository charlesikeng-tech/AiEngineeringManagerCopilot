using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.AI;

public interface IAIAnalysisEvidenceRepository
{
    Task AddRangeAsync(
        IReadOnlyList<AIAnalysisEvidence> evidence,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AIAnalysisEvidence>> GetByAnalysisIdAsync(
        Guid analysisId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}