namespace AiEngineeringManagerCopilot.Application.Jira;

public interface IJiraSyncService
{
    Task<JiraSyncResult> SyncAsync(
        Guid teamId,
        CancellationToken cancellationToken);
}