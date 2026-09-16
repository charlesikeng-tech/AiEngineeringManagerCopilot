using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class EngineeringReportRepository(
    AppDbContext dbContext)
    : IEngineeringReportRepository
{
    public async Task AddAsync(
        EngineeringReport report,
        CancellationToken cancellationToken)
    {
        await dbContext.EngineeringReports.AddAsync(
            report,
            cancellationToken);
    }

    public async Task<EngineeringReport?> GetByIdAsync(
        Guid reportId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EngineeringReports
            .FirstOrDefaultAsync(
                x =>
                    x.Id == reportId &&
                    x.TeamId == teamId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<EngineeringReport>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EngineeringReports
            .Where(x => x.TeamId == teamId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}