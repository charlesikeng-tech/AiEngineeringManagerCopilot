using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Abstractions;

namespace AiEngineeringManagerCopilot.Infrastructure.Slack;

public sealed class SlackWebhookClient(HttpClient httpClient) : ISlackWebhookClient
{
    public async Task<bool> SendAsync(
        Uri webhookUri,
        string message,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            webhookUri,
            new SlackMessage(message),
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    private sealed record SlackMessage(string Text);
}
