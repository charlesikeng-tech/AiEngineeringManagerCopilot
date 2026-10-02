namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record GitHubSyncResponse(
    int Synchronized,
    int Created,
    int Updated,
    int FailedRequests = 0)
{
    public bool IsComplete => FailedRequests == 0;
}