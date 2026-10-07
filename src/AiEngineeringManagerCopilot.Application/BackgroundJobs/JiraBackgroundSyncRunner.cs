using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Jira;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed class JiraBackgroundSyncRunner(
    IJiraConnectionRepository connectionRepository,
    IServiceScopeFactory scopeFactory,
    ILogger<JiraBackgroundSyncRunner> logger)
    : IJiraBackgroundSyncRunner
{
    public async Task<JiraBackgroundSyncResult> RunAsync(
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

            var jiraSyncService =
                scope.ServiceProvider
                    .GetRequiredService<IJiraSyncService>();

            try
            {
                await jiraSyncService.SyncAsync(
                    connection.TeamId,
                    cancellationToken);

                succeeded++;
            }
            catch (Exception exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                failed++;

                logger.LogError(
                    exception,
                    "Jira synchronization failed for team {TeamId}.",
                    connection.TeamId);
            }
        }

        return new JiraBackgroundSyncResult(
            Processed: succeeded + failed,
            Succeeded: succeeded,
            Failed: failed);
    }
}
