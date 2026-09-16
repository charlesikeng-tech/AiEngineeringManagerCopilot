using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class AIAnalysisRepository(
    AppDbContext dbContext) : IAIAnalysisRepository
{
    public async Task AddAsync(
        AIAnalysis analysis,
        CancellationToken cancellationToken)
    {
        await dbContext.AIAnalyses.AddAsync(
            analysis,
            cancellationToken);
    }

    public async Task<AIAnalysis?> GetByReportIdAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AIAnalyses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ReportId == reportId,
                cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}