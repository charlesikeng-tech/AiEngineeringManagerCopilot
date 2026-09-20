namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed record CreateJiraConnectionRequest(
    string BaseUrl,
    string Email,
    string ApiToken,
    string ProjectKey);