using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class EngineeringRiskRepository(
    AppDbContext dbContext) : IEngineeringRiskRepository
{
    public async Task AddRangeAsync(
        IReadOnlyList<EngineeringRisk> risks,
        CancellationToken cancellationToken)
    {
        await dbContext.EngineeringRisks.AddRangeAsync(
            risks,
            cancellationToken);
    }

    public async Task<IReadOnlyList<EngineeringRisk>> GetByReportIdAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EngineeringRisks
            .AsNoTracking()
            .Where(x => x.ReportId == reportId)
            .OrderByDescending(x => x.Severity)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}