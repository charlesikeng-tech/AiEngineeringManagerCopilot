namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class SlackWebhookConnection
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public string WebhookUrlEncrypted { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
