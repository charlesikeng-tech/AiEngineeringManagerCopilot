namespace AiEngineeringManagerCopilot.Infrastructure.GitHub;

public sealed class RetryDelay : IRetryDelay
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