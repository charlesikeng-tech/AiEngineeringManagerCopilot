using AiEngineeringManagerCopilot.Application.BackgroundJobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

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
            delay,
            NullLogger<GitHubSyncBackgroundService>.Instance);
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
            delay,
            NullLogger<GitHubSyncBackgroundService>.Instance);

        // Act
        await service.StartAsync(
            cancellationTokenSource.Token);

        await service.ExecuteTask!;

        // Assert
        runner.ExecutionCount.Should().Be(2);
        delay.DelayCount.Should().Be(2);
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenRunnerFails_ShouldContinueWithNextExecution()
    {
        // Arrange
        using var cancellationTokenSource =
            new CancellationTokenSource();

        var runner =
            new FailingOnceGitHubBackgroundSyncRunner();

        var delay =
            new FakeBackgroundJobDelay(
                cancellationTokenSource);

        var services = new ServiceCollection();

        services.AddScoped<IGitHubBackgroundSyncRunner>(
            _ => runner);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var service = new GitHubSyncBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            delay,
            NullLogger<GitHubSyncBackgroundService>.Instance);

        // Act
        await service.StartAsync(
            cancellationTokenSource.Token);

        await service.ExecuteTask!;

        // Assert
        runner.ExecutionCount.Should().Be(2);
        runner.SuccessCount.Should().Be(1);
        delay.DelayCount.Should().Be(2);
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenHostIsCancelled_ShouldStopGracefully()
    {
        // Arrange
        using var cancellationTokenSource =
            new CancellationTokenSource();

        var runner =
            new CancellingGitHubBackgroundSyncRunner(
                cancellationTokenSource);

        var delay =
            new FakeBackgroundJobDelay(
                cancellationTokenSource);

        var services = new ServiceCollection();

        services.AddScoped<IGitHubBackgroundSyncRunner>(
            _ => runner);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var service = new GitHubSyncBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            delay,
            NullLogger<GitHubSyncBackgroundService>.Instance);

        // Act
        await service.StartAsync(
            cancellationTokenSource.Token);

        await service.ExecuteTask!;

        // Assert
        runner.ExecutionCount.Should().Be(1);
        delay.DelayCount.Should().Be(0);
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
    
    private sealed class FailingOnceGitHubBackgroundSyncRunner
        : IGitHubBackgroundSyncRunner
    {
        public int ExecutionCount { get; private set; }

        public int SuccessCount { get; private set; }

        public Task<GitHubBackgroundSyncResult> RunAsync(
            CancellationToken cancellationToken)
        {
            ExecutionCount++;

            if (ExecutionCount == 1)
            {
                throw new HttpRequestException(
                    "GitHub is temporarily unavailable.");
            }

            SuccessCount++;

            return Task.FromResult(
                new GitHubBackgroundSyncResult(
                    Processed: 0,
                    Succeeded: 0,
                    Failed: 0));
        }
    }
    
    private sealed class CancellingGitHubBackgroundSyncRunner(
        CancellationTokenSource cancellationTokenSource)
        : IGitHubBackgroundSyncRunner
    {
        public int ExecutionCount { get; private set; }

        public Task<GitHubBackgroundSyncResult> RunAsync(
            CancellationToken cancellationToken)
        {
            ExecutionCount++;

            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(
                cancellationToken);
        }
    }
}