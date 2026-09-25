namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public interface IGitHubBackgroundSyncRunner
{
    Task<GitHubBackgroundSyncResult> RunAsync(
        CancellationToken cancellationToken);
}