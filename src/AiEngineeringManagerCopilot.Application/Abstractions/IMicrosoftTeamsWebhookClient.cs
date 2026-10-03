namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IMicrosoftTeamsWebhookClient
{
    Task<bool> SendAsync(Uri webhookUri, string message, CancellationToken cancellationToken);
}
