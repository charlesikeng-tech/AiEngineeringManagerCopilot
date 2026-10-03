namespace AiEngineeringManagerCopilot.Application.Reports;

public interface IEngineeringReportNotifier
{
    Task NotifyCreatedAsync(
        Guid teamId,
        string teamName,
        EngineeringReportResponse report,
        CancellationToken cancellationToken);
}
