namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed record JiraIssue(
    string Id,
    string Key,
    string Summary,
    string Status,
    string? AssigneeAccountId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DoneAt,
    bool IsBlocked);