using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AiEngineeringManagerCopilot.Application.BackgroundJobs;

public sealed class GitHubSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobDelay delay)
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
            await RunOnceAsync(
                stoppingToken);

            await delay.DelayAsync(
                SyncInterval,
                stoppingToken);
        }
    }
}