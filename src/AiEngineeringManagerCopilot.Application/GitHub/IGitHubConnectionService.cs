namespace AiEngineeringManagerCopilot.Application.GitHub;

public interface IGitHubConnectionService
{
    Task<GitHubConnectionResponse?> CreateAsync(
        Guid teamId,
        CreateGitHubConnectionRequest request,
        CancellationToken cancellationToken);

    Task<GitHubConnectionResponse?> GetAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid teamId,
        CancellationToken cancellationToken);
    
    Task<TestGitHubConnectionResponse?> TestAsync(
        Guid teamId,
        CancellationToken cancellationToken);
}