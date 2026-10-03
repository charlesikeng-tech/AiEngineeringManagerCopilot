using Microsoft.Extensions.Logging;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed class CompositeEngineeringReportNotifier(
    IEnumerable<IEngineeringReportNotifier> notifiers,
    ILogger<CompositeEngineeringReportNotifier> logger) : IEngineeringReportNotifier
{
    public async Task NotifyCreatedAsync(
        Guid teamId,
        string teamName,
        EngineeringReportResponse report,
        CancellationToken cancellationToken)
    {
        foreach (var notifier in notifiers)
        {
            try
            {
                await notifier.NotifyCreatedAsync(teamId, teamName, report, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception,
                    "Report notifier {Notifier} failed for team {TeamId} and report {ReportId}.",
                    notifier.GetType().Name, teamId, report.Id);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(exception,
                    "Report notifier {Notifier} timed out for team {TeamId} and report {ReportId}.",
                    notifier.GetType().Name, teamId, report.Id);
            }
        }
    }
}
