using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record GitHubConnectionResponse(
    Guid Id,
    Guid TeamId,
    string Organization,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSyncAt)
{
    public static GitHubConnectionResponse FromEntity(
        GitHubConnection connection)
    {
        return new GitHubConnectionResponse(
            connection.Id,
            connection.TeamId,
            connection.Organization,
            connection.CreatedAt,
            connection.LastSyncAt);
    }
}