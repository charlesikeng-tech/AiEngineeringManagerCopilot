using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;

namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed class GitHubBackgroundSyncRunner(
    IGitHubConnectionRepository connectionRepository,
    IGitHubSyncService gitHubSyncService)
    : IGitHubBackgroundSyncRunner
{
    public async Task<GitHubBackgroundSyncResult> RunAsync(
        CancellationToken cancellationToken)
    {
        var connections =
            await connectionRepository.GetAllAsync(
                cancellationToken);

        var succeeded = 0;
        var failed = 0;

        foreach (var connection in connections)
        {
            try
            {
                var result = await gitHubSyncService.SyncAsync(
                    connection.TeamId,
                    cancellationToken);

                if (result is null)
                {
                    failed++;
                    continue;
                }

                succeeded++;
            }
            catch (HttpRequestException)
            {
                failed++;
            }
        }

        return new GitHubBackgroundSyncResult(
            Processed: succeeded + failed,
            Succeeded: succeeded,
            Failed: failed);
    }
}