using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Reports;

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

    public async Task<bool> SendReportAsync(
        Uri webhookUri,
        ReportNotification notification,
        CancellationToken cancellationToken)
    {
        var indicator = notification.HealthLevel switch
        {
            "Excellent" or "Healthy" => ":large_green_circle:",
            "Needs Attention" or "At Risk" => ":large_yellow_circle:",
            "Critical" => ":red_circle:",
            _ => ":white_circle:"
        };
        var blocks = new List<object>
        {
            new { type = "header", text = PlainText("Engineering Health Report") },
            new
            {
                type = "section",
                text = new { type = "mrkdwn", text = $"{indicator} *{notification.OverallScore}/100* | {EscapeText(notification.HealthLevel)}" }
            },
            new
            {
                type = "section",
                fields = new[]
                {
                    PlainText($"Team\n{notification.TeamName}"),
                    PlainText($"Period\n{notification.Period}"),
                    PlainText($"Data coverage\n{notification.Coverage}"),
                    PlainText($"Detected risks: {notification.RiskCount}\nRecommended actions: {notification.ActionCount}")
                }
            },
            new { type = "divider" },
            new { type = "section", text = new { type = "mrkdwn", text = "*Executive summary*" } },
            new
            {
                type = "section",
                text = PlainText(string.IsNullOrWhiteSpace(notification.ExecutiveSummary)
                    ? "No executive summary recorded." : notification.ExecutiveSummary)
            }
        };

        AddItems(blocks, "Top risks", notification.Risks, "No risks recorded in this report.");
        AddItems(blocks, "Priority actions", notification.Actions, "No open recommended actions recorded.");
        blocks.Add(new
        {
            type = "actions",
            elements = new[]
            {
                new
                {
                    type = "button", action_id = "view_report", style = "primary",
                    text = PlainText("View report"), url = notification.ReportUrl.AbsoluteUri
                }
            }
        });

        using var response = await httpClient.PostAsJsonAsync(webhookUri, new
        {
            text = EscapeText(notification.FallbackText),
            parse = "none",
            blocks,
            unfurl_links = false,
            unfurl_media = false
        }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static void AddItems(
        List<object> blocks, string title, IReadOnlyList<ReportNotificationItem> items, string emptyMessage)
    {
        blocks.Add(new { type = "divider" });
        blocks.Add(new { type = "section", text = new { type = "mrkdwn", text = $"*{title}*" } });
        if (items.Count == 0)
        {
            blocks.Add(new { type = "section", text = PlainText(emptyMessage) });
        }
        foreach (var item in items)
        {
            blocks.Add(new { type = "section", text = PlainText($"[{item.Priority}] {item.Title}\n{item.Description}") });
        }
    }

    private static object PlainText(string text) => new { type = "plain_text", text };

    private static string EscapeText(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
