using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IEngineeringActionRepository
{
    Task AddRangeAsync(
        IEnumerable<EngineeringAction> actions,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringAction>> GetByReportIdAsync(
        Guid reportId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}