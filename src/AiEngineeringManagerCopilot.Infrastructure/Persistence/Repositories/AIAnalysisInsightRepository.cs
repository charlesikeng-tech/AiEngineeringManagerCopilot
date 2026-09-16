using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class AIAnalysisInsightRepository(
    AppDbContext dbContext) : IAIAnalysisInsightRepository
{
    public async Task AddRangeAsync(
        IReadOnlyList<AIAnalysisInsight> insights,
        CancellationToken cancellationToken)
    {
        await dbContext.AIAnalysisInsights.AddRangeAsync(
            insights,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AIAnalysisInsight>> GetByAnalysisIdAsync(
        Guid analysisId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AIAnalysisInsights
            .AsNoTracking()
            .Where(x => x.AIAnalysisId == analysisId)
            .OrderBy(x => x.Title)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}