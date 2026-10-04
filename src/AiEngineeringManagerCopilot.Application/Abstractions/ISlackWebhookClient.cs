using AiEngineeringManagerCopilot.Application.Reports;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface ISlackWebhookClient
{
    Task<bool> SendAsync(
        Uri webhookUri,
        string message,
        CancellationToken cancellationToken);

    Task<bool> SendReportAsync(
        Uri webhookUri,
        ReportNotification notification,
        CancellationToken cancellationToken);
}
