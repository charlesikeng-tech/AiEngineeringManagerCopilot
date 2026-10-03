using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Abstractions;

namespace AiEngineeringManagerCopilot.Infrastructure.MicrosoftTeams;

public sealed class MicrosoftTeamsWebhookClient(HttpClient httpClient) : IMicrosoftTeamsWebhookClient
{
    public async Task<bool> SendAsync(
        Uri webhookUri, string message, CancellationToken cancellationToken)
    {
        var payload = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    contentUrl = (string?)null,
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.2",
                        body = new[] { new { type = "TextBlock", text = message, wrap = true } }
                    }
                }
            }
        };

        using var response = await httpClient.PostAsJsonAsync(
            webhookUri, payload, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
