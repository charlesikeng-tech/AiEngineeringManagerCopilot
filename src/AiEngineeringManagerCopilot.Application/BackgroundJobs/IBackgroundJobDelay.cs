namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public interface IBackgroundJobDelay
{
    Task DelayAsync(
        TimeSpan delay,
        CancellationToken cancellationToken);
}