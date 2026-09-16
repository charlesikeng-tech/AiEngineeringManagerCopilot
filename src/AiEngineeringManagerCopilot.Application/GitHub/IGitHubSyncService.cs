namespace AiEngineeringManagerCopilot.Application.GitHub;

public interface IGitHubSyncService
{
    Task<GitHubSyncResponse?> SyncAsync(
        Guid teamId,
        CancellationToken cancellationToken);
}