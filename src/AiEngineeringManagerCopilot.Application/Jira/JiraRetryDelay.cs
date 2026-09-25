namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed class JiraRetryDelay : IJiraRetryDelay
{
    public Task DelayAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        return Task.Delay(
            delay,
            cancellationToken);
    }
}