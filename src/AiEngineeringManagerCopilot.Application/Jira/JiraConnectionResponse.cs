using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed record JiraConnectionResponse(
    Guid Id,
    Guid TeamId,
    string BaseUrl,
    string Email,
    string ProjectKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSyncAt)
{
    public static JiraConnectionResponse FromEntity(
        JiraConnection connection)
    {
        return new JiraConnectionResponse(
            connection.Id,
            connection.TeamId,
            connection.BaseUrl,
            connection.Email,
            connection.ProjectKey,
            connection.CreatedAt,
            connection.LastSyncAt);
    }
}