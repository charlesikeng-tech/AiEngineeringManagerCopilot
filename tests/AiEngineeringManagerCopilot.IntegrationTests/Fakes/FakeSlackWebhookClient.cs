using AiEngineeringManagerCopilot.Application.Abstractions;

namespace AiEngineeringManagerCopilot.IntegrationTests.Fakes;

public sealed class FakeSlackWebhookClient : ISlackWebhookClient
{
    public bool Result { get; set; } = true;

    public Exception? Failure { get; set; }

    public Uri? LastWebhookUri { get; private set; }

    public string? LastMessage { get; private set; }

    public Task<bool> SendAsync(
        Uri webhookUri,
        string message,
        CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        LastWebhookUri = webhookUri;
        LastMessage = message;
        return Task.FromResult(Result);
    }

    public void Reset()
    {
        Result = true;
        Failure = null;
        LastWebhookUri = null;
        LastMessage = null;
    }
}
