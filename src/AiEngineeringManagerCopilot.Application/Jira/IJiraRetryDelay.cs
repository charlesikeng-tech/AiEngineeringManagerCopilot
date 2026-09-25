namespace AiEngineeringManagerCopilot.Application.Jira;

public interface IJiraRetryDelay
{
    Task DelayAsync(
        TimeSpan delay,
        CancellationToken cancellationToken);
}