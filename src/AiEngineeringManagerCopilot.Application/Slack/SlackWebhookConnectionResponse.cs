namespace AiEngineeringManagerCopilot.Application.Slack;

public sealed record SlackWebhookConnectionResponse(
    Guid TeamId,
    DateTimeOffset CreatedAt);
