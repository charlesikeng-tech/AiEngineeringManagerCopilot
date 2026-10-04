using AiEngineeringManagerCopilot.Application.Reports;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IMicrosoftTeamsWebhookClient
{
    Task<bool> SendAsync(Uri webhookUri, string message, CancellationToken cancellationToken);

    Task<bool> SendReportAsync(Uri webhookUri, ReportNotification notification, CancellationToken cancellationToken);
}
