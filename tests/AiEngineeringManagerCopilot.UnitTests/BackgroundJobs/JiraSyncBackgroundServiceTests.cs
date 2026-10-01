using AiEngineeringManagerCopilot.Application.BackgroundJobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiEngineeringManagerCopilot.UnitTests.BackgroundJobs;

public sealed class JiraSyncBackgroundServiceTests
{
    [Fact]
    public async Task RunOnceAsync_ShouldExecuteJiraBackgroundSyncRunner()
    {
        // Arrange
        var runner = new FakeJiraBackgroundSyncRunner();

        var services = new ServiceCollection();
        services.AddScoped<IJiraBackgroundSyncRunner>(_ => runner);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var delay = new FakeBackgroundJobDelay(
            new CancellationTokenSource());

        var service = new JiraSyncBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            delay,
            NullLogger<JiraSyncBackgroundService>.Instance);

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
        var runner = new FakeJiraBackgroundSyncRunner();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var delay =
            new FakeBackgroundJobDelay(
                cancellationTokenSource);

        var services = new ServiceCollection();
        services.AddScoped<IJiraBackgroundSyncRunner>(_ => runner);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var service = new JiraSyncBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            delay,
            NullLogger<JiraSyncBackgroundService>.Instance);

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
            new FailingOnceJiraBackgroundSyncRunner();

        var delay =
            new FakeBackgroundJobDelay(
                cancellationTokenSource);

        var services = new ServiceCollection();

        services.AddScoped<IJiraBackgroundSyncRunner>(
            _ => runner);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var service = new JiraSyncBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            delay,
            NullLogger<JiraSyncBackgroundService>.Instance);

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
            new CancellingJiraBackgroundSyncRunner(
                cancellationTokenSource);

        var delay =
            new FakeBackgroundJobDelay(
                cancellationTokenSource);

        var services = new ServiceCollection();

        services.AddScoped<IJiraBackgroundSyncRunner>(
            _ => runner);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var service = new JiraSyncBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            delay,
            NullLogger<JiraSyncBackgroundService>.Instance);

        // Act
        await service.StartAsync(
            cancellationTokenSource.Token);

        await service.ExecuteTask!;

        // Assert
        runner.ExecutionCount.Should().Be(1);
        delay.DelayCount.Should().Be(0);
    }

    private sealed class FakeJiraBackgroundSyncRunner
        : IJiraBackgroundSyncRunner
    {
        public int ExecutionCount { get; private set; }

        public Task<JiraBackgroundSyncResult> RunAsync(
            CancellationToken cancellationToken)
        {
            ExecutionCount++;

            return Task.FromResult(
                new JiraBackgroundSyncResult(
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
    
    private sealed class FailingOnceJiraBackgroundSyncRunner
        : IJiraBackgroundSyncRunner
    {
        public int ExecutionCount { get; private set; }

        public int SuccessCount { get; private set; }

        public Task<JiraBackgroundSyncResult> RunAsync(
            CancellationToken cancellationToken)
        {
            ExecutionCount++;

            if (ExecutionCount == 1)
            {
                throw new HttpRequestException(
                    "Jira is temporarily unavailable.");
            }

            SuccessCount++;

            return Task.FromResult(
                new JiraBackgroundSyncResult(
                    Processed: 0,
                    Succeeded: 0,
                    Failed: 0));
        }
    }
    
    private sealed class CancellingJiraBackgroundSyncRunner(
        CancellationTokenSource cancellationTokenSource)
        : IJiraBackgroundSyncRunner
    {
        public int ExecutionCount { get; private set; }

        public Task<JiraBackgroundSyncResult> RunAsync(
            CancellationToken cancellationToken)
        {
            ExecutionCount++;

            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(
                cancellationToken);
        }
    }
}