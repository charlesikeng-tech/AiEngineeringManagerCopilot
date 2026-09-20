namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed record JiraCurrentUser(
    string AccountId,
    string DisplayName,
    string EmailAddress);