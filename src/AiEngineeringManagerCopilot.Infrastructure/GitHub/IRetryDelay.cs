namespace AiEngineeringManagerCopilot.Infrastructure.GitHub;

public interface IRetryDelay
{
    Task DelayAsync(
        TimeSpan delay,
        CancellationToken cancellationToken);
}