namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed record GitHubBackgroundSyncResult(
    int Processed,
    int Succeeded,
    int Failed);