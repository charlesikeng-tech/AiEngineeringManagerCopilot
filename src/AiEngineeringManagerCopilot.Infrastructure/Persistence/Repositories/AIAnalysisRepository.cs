using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class AIAnalysisRepository(
    AppDbContext dbContext) : IAIAnalysisRepository
{
    public async Task<IAsyncDisposable> AcquireReportLockAsync(
        Guid reportId, CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_lock(hashtextextended({reportId.ToString()}, 0));",
                cancellationToken);
            return new ReportLock(dbContext, reportId);
        }
        catch
        {
            await dbContext.Database.CloseConnectionAsync();
            throw;
        }
    }

    private sealed class ReportLock(AppDbContext context, Guid reportId) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_unlock(hashtextextended({reportId.ToString()}, 0));");
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

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