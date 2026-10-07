using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.BackgroundJobs;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiEngineeringManagerCopilot.UnitTests.BackgroundJobs;

public sealed class JiraBackgroundSyncRunnerTests
{
    [Fact]
    public async Task RunAsync_ShouldSyncAllJiraConnections()
    {
        // Arrange
        var firstTeamId = Guid.NewGuid();
        var secondTeamId = Guid.NewGuid();

        var connectionRepository =
            new FakeJiraConnectionRepository(
            [
                CreateConnection(firstTeamId),
                CreateConnection(secondTeamId)
            ]);

        var jiraSyncService =
            new FakeJiraSyncService();

        var runner =
            CreateRunner(
                connectionRepository,
                jiraSyncService);

        // Act
        var result = await runner.RunAsync(
            CancellationToken.None);

        // Assert
        result.Processed.Should().Be(2);
        result.Succeeded.Should().Be(2);
        result.Failed.Should().Be(0);

        jiraSyncService.SyncedTeamIds.Should()
            .BeEquivalentTo(
                [firstTeamId, secondTeamId]);
    }
    
    [Fact]
    public async Task RunAsync_WhenOneSyncFails_ShouldContinueWithOtherConnections()
    {
        // Arrange
        var firstTeamId = Guid.NewGuid();
        var secondTeamId = Guid.NewGuid();

        var connectionRepository =
            new FakeJiraConnectionRepository(
            [
                CreateConnection(firstTeamId),
                CreateConnection(secondTeamId)
            ]);

        var jiraSyncService =
            new FakeJiraSyncService
            {
                FailingTeamId = firstTeamId
            };

        var runner =
            CreateRunner(
                connectionRepository,
                jiraSyncService);

        // Act
        var result = await runner.RunAsync(
            CancellationToken.None);

        // Assert
        jiraSyncService.SyncedTeamIds.Should()
            .ContainInOrder(
                firstTeamId,
                secondTeamId);

        result.Processed.Should().Be(2);
        result.Succeeded.Should().Be(1);
        result.Failed.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_WhenSyncTimesOut_ShouldContinueWithOtherConnections()
    {
        // Arrange
        var firstTeamId = Guid.NewGuid();
        var secondTeamId = Guid.NewGuid();

        var connectionRepository =
            new FakeJiraConnectionRepository(
            [
                CreateConnection(firstTeamId),
                CreateConnection(secondTeamId)
            ]);

        // HttpClient reports its timeout as a TaskCanceledException,
        // not as an HttpRequestException.
        var jiraSyncService =
            new FakeJiraSyncService
            {
                FailingTeamId = firstTeamId,
                ExceptionToThrow = new TaskCanceledException("Timeout")
            };

        var runner =
            CreateRunner(
                connectionRepository,
                jiraSyncService);

        // Act
        var result = await runner.RunAsync(
            CancellationToken.None);

        // Assert
        jiraSyncService.SyncedTeamIds.Should()
            .ContainInOrder(
                firstTeamId,
                secondTeamId);

        result.Processed.Should().Be(2);
        result.Succeeded.Should().Be(1);
        result.Failed.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_ShouldResolveSyncServiceInADedicatedScopePerConnection()
    {
        // Arrange
        var connectionRepository =
            new FakeJiraConnectionRepository(
            [
                CreateConnection(Guid.NewGuid()),
                CreateConnection(Guid.NewGuid())
            ]);

        var jiraSyncService =
            new FakeJiraSyncService();

        var runner =
            CreateRunner(
                connectionRepository,
                jiraSyncService);

        // Act
        await runner.RunAsync(
            CancellationToken.None);

        // Assert
        jiraSyncService.ScopeCount.Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_ShouldStopAndPropagateCancellation()
    {
        // Arrange
        var firstTeamId = Guid.NewGuid();

        var connectionRepository =
            new FakeJiraConnectionRepository(
            [
                CreateConnection(firstTeamId),
                CreateConnection(Guid.NewGuid())
            ]);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        await cancellationTokenSource.CancelAsync();

        var jiraSyncService =
            new FakeJiraSyncService
            {
                FailingTeamId = firstTeamId,
                ExceptionToThrow = new OperationCanceledException(
                    cancellationTokenSource.Token)
            };

        var runner =
            CreateRunner(
                connectionRepository,
                jiraSyncService);

        // Act
        var act = () => runner.RunAsync(
            cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        jiraSyncService.SyncedTeamIds.Should()
            .Equal(firstTeamId);
    }

    private static JiraBackgroundSyncRunner CreateRunner(
        IJiraConnectionRepository connectionRepository,
        FakeJiraSyncService jiraSyncService)
    {
        var services = new ServiceCollection();

        services.AddScoped<IJiraSyncService>(_ =>
        {
            jiraSyncService.ScopeCount++;

            return jiraSyncService;
        });

        var serviceProvider = services.BuildServiceProvider();

        return new JiraBackgroundSyncRunner(
            connectionRepository,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<JiraBackgroundSyncRunner>.Instance);
    }

    private static JiraConnection CreateConnection(
        Guid teamId)
    {
        return new JiraConnection
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            BaseUrl = "https://jira.example.com",
            Email = "team@example.com",
            ApiTokenEncrypted = "encrypted-token",
            ProjectKey = "TEST",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class FakeJiraConnectionRepository(
        IReadOnlyList<JiraConnection> connections)
        : IJiraConnectionRepository
    {
        public Task<IReadOnlyList<JiraConnection>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(connections);
        }

        public Task<JiraConnection?> GetByTeamIdAsync(
            Guid teamId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                connections.FirstOrDefault(
                    x => x.TeamId == teamId));
        }

        public Task AddAsync(
            JiraConnection connection,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(
            JiraConnection connection,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeJiraSyncService
        : IJiraSyncService
    {
        public List<Guid> SyncedTeamIds { get; } = [];

        public int ScopeCount { get; set; }

        public Guid? FailingTeamId { get; set; }

        public Exception ExceptionToThrow { get; set; } =
            new HttpRequestException("Jira unavailable");

        public Task<JiraSyncResult> SyncAsync(
            Guid teamId,
            CancellationToken cancellationToken)
        {
            SyncedTeamIds.Add(teamId);

            if (teamId == FailingTeamId)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(
                new JiraSyncResult(
                    Created: 0,
                    Updated: 0,
                    Total: 0));
        }
    }
}