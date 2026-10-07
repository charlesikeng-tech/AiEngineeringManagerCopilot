using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed class GitHubBackgroundSyncRunner(
    IGitHubConnectionRepository connectionRepository,
    IServiceScopeFactory scopeFactory,
    ILogger<GitHubBackgroundSyncRunner> logger)
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
            // One scope per team: a failed sync must not leave tracked
            // entities in a DbContext reused by the next team.
            using var scope = scopeFactory.CreateScope();

            var gitHubSyncService =
                scope.ServiceProvider
                    .GetRequiredService<IGitHubSyncService>();

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
            catch (Exception exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                failed++;

                logger.LogError(
                    exception,
                    "GitHub synchronization failed for team {TeamId}.",
                    connection.TeamId);
            }
        }

        return new GitHubBackgroundSyncResult(
            Processed: succeeded + failed,
            Succeeded: succeeded,
            Failed: failed);
    }
}
