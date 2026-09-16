using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class AIAnalysisActionRepository(
    AppDbContext dbContext) : IAIAnalysisActionRepository
{
    public async Task AddRangeAsync(
        IReadOnlyList<AIAnalysisAction> actions,
        CancellationToken cancellationToken)
    {
        await dbContext.AIAnalysisActions.AddRangeAsync(
            actions,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AIAnalysisAction>> GetByAnalysisIdAsync(
        Guid analysisId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AIAnalysisActions
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