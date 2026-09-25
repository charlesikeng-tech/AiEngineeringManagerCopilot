using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Jira;

namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed class JiraBackgroundSyncRunner(
    IJiraConnectionRepository connectionRepository,
    IJiraSyncService jiraSyncService)
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
            try
            {
                await jiraSyncService.SyncAsync(
                    connection.TeamId,
                    cancellationToken);

                succeeded++;
            }
            catch (HttpRequestException)
            {
                failed++;
            }
        }

        return new JiraBackgroundSyncResult(
            Processed: succeeded + failed,
            Succeeded: succeeded,
            Failed: failed);
    }
}