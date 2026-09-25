namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed record JiraBackgroundSyncResult(
    int Processed,
    int Succeeded,
    int Failed);