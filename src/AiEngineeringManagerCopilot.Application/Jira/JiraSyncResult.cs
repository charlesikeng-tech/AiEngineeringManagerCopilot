namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed record JiraSyncResult(
    int Created,
    int Updated,
    int Total);