namespace AiEngineeringManagerCopilot.Application.Slack;

public sealed record TestSlackWebhookResponse(
    bool Success,
    string Message);
