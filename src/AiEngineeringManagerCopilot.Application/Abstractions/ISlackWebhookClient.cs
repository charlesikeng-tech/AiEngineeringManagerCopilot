namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface ISlackWebhookClient
{
    Task<bool> SendAsync(
        Uri webhookUri,
        string message,
        CancellationToken cancellationToken);
}
