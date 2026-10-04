using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Reports;

namespace AiEngineeringManagerCopilot.IntegrationTests.Fakes;

public sealed class FakeMicrosoftTeamsWebhookClient : IMicrosoftTeamsWebhookClient
{
    public bool Result { get; set; } = true;

    public Exception? Failure { get; set; }

    public Uri? LastWebhookUri { get; private set; }

    public string? LastMessage { get; private set; }
    public ReportNotification? LastReport { get; private set; }

    public Task<bool> SendReportAsync(Uri webhookUri, ReportNotification notification, CancellationToken cancellationToken)
    {
        LastReport = notification;
        return SendAsync(webhookUri, notification.FallbackText, cancellationToken);
    }

    public Task<bool> SendAsync(
        Uri webhookUri, string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastWebhookUri = webhookUri;
        LastMessage = message;
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(Result);
    }

    public void Reset()
    {
        Result = true;
        Failure = null;
        LastWebhookUri = null;
        LastMessage = null;
        LastReport = null;
    }
}
