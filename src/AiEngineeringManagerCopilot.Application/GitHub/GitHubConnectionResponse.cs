using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record GitHubConnectionResponse(
    Guid Id,
    Guid TeamId,
    string Owner,
    GitHubOwnerType OwnerType,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSyncAt)
{
    public static GitHubConnectionResponse FromEntity(
        GitHubConnection connection)
    {
        return new GitHubConnectionResponse(
            connection.Id,
            connection.TeamId,
            connection.Owner,
            connection.OwnerType,
            connection.CreatedAt,
            connection.LastSyncAt);
    }
}