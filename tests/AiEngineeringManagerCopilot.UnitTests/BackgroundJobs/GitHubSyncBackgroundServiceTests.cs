using AiEngineeringManagerCopilot.Application.BackgroundJobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.UnitTests.BackgroundJobs;

public sealed class GitHubSyncBackgroundServiceTests
{
    [Fact]
    public async Task RunOnceAsync_ShouldExecuteGitHubBackgroundSyncRunner()
    {
        // Arrange
        var runner = new FakeGitHubBackgroundSyncRunner();

        var services = new ServiceCollection();

        services.AddScoped<IGitHubBackgroundSyncRunner>(
            _ => runner);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var delay = new FakeBackgroundJobDelay(
            new CancellationTokenSource());

        var service = new GitHubSyncBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            delay);
        // Act
        await service.RunOnceAsync(
            CancellationToken.None);

        // Assert
        runner.ExecutionCount.Should().Be(1);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldRunPeriodically()
    {
        // Arrange
        var runner = new FakeGitHubBackgroundSyncRunner();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var delay = new FakeBackgroundJobDelay(
            cancellationTokenSource);

        var services = new ServiceCollection();

        services.AddScoped<IGitHubBackgroundSyncRunner>(
            _ => runner);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var service = new GitHubSyncBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            delay);

        // Act
        await service.StartAsync(
            cancellationTokenSource.Token);

        await service.ExecuteTask!;

        // Assert
        runner.ExecutionCount.Should().Be(2);
        delay.DelayCount.Should().Be(2);
    }

    private sealed class FakeGitHubBackgroundSyncRunner
        : IGitHubBackgroundSyncRunner
    {
        public int ExecutionCount { get; private set; }

        public Task<GitHubBackgroundSyncResult> RunAsync(
            CancellationToken cancellationToken)
        {
            ExecutionCount++;

            return Task.FromResult(
                new GitHubBackgroundSyncResult(
                    Processed: 0,
                    Succeeded: 0,
                    Failed: 0));
        }
    }
    
    private sealed class FakeBackgroundJobDelay(
        CancellationTokenSource cancellationTokenSource)
        : IBackgroundJobDelay
    {
        public int DelayCount { get; private set; }

        public Task DelayAsync(
            TimeSpan delay,
            CancellationToken cancellationToken)
        {
            DelayCount++;

            if (DelayCount >= 2)
            {
                cancellationTokenSource.Cancel();
            }

            return Task.CompletedTask;
        }
    }
}