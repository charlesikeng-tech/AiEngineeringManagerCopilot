namespace AiEngineeringManagerCopilot.Application.Jira;

public interface IJiraConnectionService
{
    Task<JiraConnectionResponse?> CreateAsync(
        Guid teamId,
        CreateJiraConnectionRequest request,
        CancellationToken cancellationToken);

    Task<JiraConnectionResponse?> GetAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid teamId,
        CancellationToken cancellationToken);
    
    Task<TestJiraConnectionResponse?> TestAsync(
        Guid teamId,
        CancellationToken cancellationToken);
}