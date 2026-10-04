using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Reports;

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

    public async Task<bool> SendReportAsync(
        Uri webhookUri, ReportNotification notification, CancellationToken cancellationToken)
    {
        var scoreColor = notification.HealthLevel switch
        {
            "Excellent" or "Healthy" => "Good",
            "Needs Attention" or "At Risk" => "Warning",
            "Critical" => "Attention",
            _ => "Default"
        };
        var body = new List<object>
        {
            new { type = "TextBlock", text = "Engineering Health Report", size = "Large", weight = "Bolder", wrap = true },
            new
            {
                type = "TextBlock", text = $"{notification.OverallScore}/100 - {EscapeMarkdown(notification.HealthLevel)}",
                size = "ExtraLarge", weight = "Bolder", color = scoreColor, wrap = true
            },
            new
            {
                type = "FactSet",
                facts = new[]
                {
                    new { title = "Team", value = EscapeMarkdown(notification.TeamName) },
                    new { title = "Period", value = notification.Period },
                    new { title = "Data coverage", value = notification.Coverage },
                    new { title = "Detected risks", value = notification.RiskCount.ToString() },
                    new { title = "Recommended actions", value = notification.ActionCount.ToString() }
                }
            },
            new { type = "TextBlock", text = "Executive summary", weight = "Bolder", separator = true, wrap = true },
            new
            {
                type = "TextBlock",
                text = EscapeMarkdown(string.IsNullOrWhiteSpace(notification.ExecutiveSummary)
                    ? "No executive summary recorded." : notification.ExecutiveSummary),
                wrap = true
            }
        };
        AddItems(body, "Top risks", notification.Risks, "No risks recorded in this report.");
        AddItems(body, "Priority actions", notification.Actions, "No open recommended actions recorded.");
        using var response = await httpClient.PostAsJsonAsync(webhookUri, new
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
                        type = "AdaptiveCard", version = "1.2", body,
                        actions = new[]
                        {
                            new { type = "Action.OpenUrl", title = "View report", url = notification.ReportUrl.AbsoluteUri }
                        }
                    }
                }
            }
        }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static void AddItems(
        List<object> body, string title, IReadOnlyList<ReportNotificationItem> items, string emptyMessage)
    {
        body.Add(new { type = "TextBlock", text = title, weight = "Bolder", separator = true, wrap = true });
        if (items.Count == 0)
        {
            body.Add(new { type = "TextBlock", text = emptyMessage, wrap = true });
        }
        foreach (var item in items)
        {
            body.Add(new
            {
                type = "TextBlock",
                text = $"**[{item.Priority}] {EscapeMarkdown(item.Title)}**\n\n{EscapeMarkdown(item.Description)}",
                wrap = true
            });
        }
    }

    private static string EscapeMarkdown(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
