namespace AiEngineeringManagerCopilot.Application.Reports;

public interface IEngineeringReportService
{
    Task<EngineeringReportResponse?> GenerateAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);

    Task<EngineeringReportResponse?> GetByIdAsync(
        Guid teamId,
        Guid reportId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringReportResponse>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken);
}