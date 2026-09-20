namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed record TestJiraConnectionResponse(
    bool IsValid,
    string? DisplayName,
    string Message);