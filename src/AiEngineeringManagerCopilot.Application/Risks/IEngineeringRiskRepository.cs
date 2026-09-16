using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Risks;

public interface IEngineeringRiskRepository
{
    Task AddRangeAsync(
        IReadOnlyList<EngineeringRisk> risks,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringRisk>> GetByReportIdAsync(
        Guid reportId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}