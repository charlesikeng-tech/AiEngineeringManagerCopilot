using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class EngineeringReportInsightRepository(
    AppDbContext dbContext)
    : IEngineeringReportInsightRepository
{
    public async Task AddRangeAsync(
        IEnumerable<EngineeringReportInsight> insights,
        CancellationToken cancellationToken)
    {
        await dbContext.EngineeringReportInsights.AddRangeAsync(
            insights,
            cancellationToken);
    }

    public async Task<IReadOnlyList<EngineeringReportInsight>>
        GetByReportIdAsync(
            Guid reportId,
            CancellationToken cancellationToken)
    {
        return await dbContext.EngineeringReportInsights
            .Where(x => x.ReportId == reportId)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}