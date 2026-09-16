namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record TestGitHubConnectionResponse(
    bool Success,
    string Organization,
    string Message);