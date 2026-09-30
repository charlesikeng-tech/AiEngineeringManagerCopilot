using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record CreateGitHubConnectionRequest(
    string Owner,
    GitHubOwnerType OwnerType,
    string AccessToken);