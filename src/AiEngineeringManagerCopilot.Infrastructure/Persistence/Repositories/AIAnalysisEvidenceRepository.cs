using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class AIAnalysisEvidenceRepository(
    AppDbContext dbContext) : IAIAnalysisEvidenceRepository
{
    public async Task AddRangeAsync(
        IReadOnlyList<AIAnalysisEvidence> evidence,
        CancellationToken cancellationToken)
    {
        await dbContext.AIAnalysisEvidence.AddRangeAsync(
            evidence,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AIAnalysisEvidence>> GetByAnalysisIdAsync(
        Guid analysisId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AIAnalysisEvidence
            .AsNoTracking()
            .Where(x => x.AIAnalysisId == analysisId)
            .OrderBy(x => x.MetricType)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}