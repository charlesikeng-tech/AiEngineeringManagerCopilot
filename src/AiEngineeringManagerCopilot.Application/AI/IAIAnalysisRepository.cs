using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.AI;

public interface IAIAnalysisRepository
{
    Task AddAsync(
        AIAnalysis analysis,
        CancellationToken cancellationToken);

    Task<AIAnalysis?> GetByReportIdAsync(
        Guid reportId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}