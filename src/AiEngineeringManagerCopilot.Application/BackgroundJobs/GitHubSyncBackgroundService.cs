using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed class GitHubSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobDelay delay,
    ILogger<GitHubSyncBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan SyncInterval =
        TimeSpan.FromMinutes(15);

    public async Task RunOnceAsync(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var runner =
            scope.ServiceProvider
                .GetRequiredService<IGitHubBackgroundSyncRunner>();

        await runner.RunAsync(
            cancellationToken);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "GitHub background synchronization failed.");
            }

            try
            {
                await delay.DelayAsync(
                    SyncInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}