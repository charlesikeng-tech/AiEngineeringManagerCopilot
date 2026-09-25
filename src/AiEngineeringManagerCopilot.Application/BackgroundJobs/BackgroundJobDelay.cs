namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed class BackgroundJobDelay
    : IBackgroundJobDelay
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