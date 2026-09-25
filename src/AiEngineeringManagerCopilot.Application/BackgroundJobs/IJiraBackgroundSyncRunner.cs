namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public interface IJiraBackgroundSyncRunner
{
    Task<JiraBackgroundSyncResult> RunAsync(
        CancellationToken cancellationToken);
}