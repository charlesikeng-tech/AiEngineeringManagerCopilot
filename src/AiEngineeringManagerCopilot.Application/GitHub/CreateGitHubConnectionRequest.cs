namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record CreateGitHubConnectionRequest(
    string Organization,
    string AccessToken);