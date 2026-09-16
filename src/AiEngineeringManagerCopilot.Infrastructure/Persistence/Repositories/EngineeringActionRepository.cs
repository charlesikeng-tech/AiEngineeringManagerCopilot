using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class EngineeringActionRepository(
    AppDbContext dbContext)
    : IEngineeringActionRepository
{
    public async Task AddRangeAsync(
        IEnumerable<EngineeringAction> actions,
        CancellationToken cancellationToken)
    {
        await dbContext.EngineeringActions.AddRangeAsync(
            actions,
            cancellationToken);
    }

    public async Task<IReadOnlyList<EngineeringAction>> GetByReportIdAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var actions = await dbContext.EngineeringActions
            .Where(x => x.ReportId == reportId)
            .ToListAsync(cancellationToken);

        return actions
            .OrderBy(x => x.Status switch
            {
                ActionStatus.Todo => 0,
                ActionStatus.InProgress => 1,
                ActionStatus.Done => 2,
                ActionStatus.Cancelled => 3,
                _ => 99
            })
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.Title)
            .ToList();
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}