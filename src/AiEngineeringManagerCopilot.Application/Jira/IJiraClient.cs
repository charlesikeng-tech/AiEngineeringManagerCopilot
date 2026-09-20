namespace AiEngineeringManagerCopilot.Application.Jira;

public interface IJiraClient
{
    Task<JiraCurrentUser?> GetCurrentUserAsync(
        string baseUrl,
        string email,
        string apiToken,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<JiraIssue>> GetIssuesAsync(
        string baseUrl,
        string email,
        string apiToken,
        string projectKey,
        CancellationToken cancellationToken);
}