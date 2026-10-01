using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed class JiraSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobDelay delay,
    ILogger<JiraSyncBackgroundService> logger)
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
                .GetRequiredService<IJiraBackgroundSyncRunner>();

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
                    "Jira background synchronization failed.");
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