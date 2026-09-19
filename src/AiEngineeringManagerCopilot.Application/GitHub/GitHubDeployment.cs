namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record GitHubDeployment(
    long Id,
    string Environment,
    DateTimeOffset CreatedAt);