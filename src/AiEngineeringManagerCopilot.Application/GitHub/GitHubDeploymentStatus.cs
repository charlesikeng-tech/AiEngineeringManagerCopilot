namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record GitHubDeploymentStatus(
    long Id,
    string State,
    DateTimeOffset CreatedAt);