using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IEngineeringReportInsightRepository
{
    Task AddRangeAsync(
        IEnumerable<EngineeringReportInsight> insights,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringReportInsight>> GetByReportIdAsync(
        Guid reportId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}