namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record GitHubSyncResponse(
    int Synchronized,
    int Created,
    int Updated);