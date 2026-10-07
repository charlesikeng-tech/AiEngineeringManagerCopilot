using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.BackgroundJobs;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiEngineeringManagerCopilot.UnitTests.BackgroundJobs;

public sealed class GitHubBackgroundSyncRunnerTests
{
    [Fact]
    public async Task RunAsync_ShouldSyncAllGitHubConnections()
    {
        // Arrange
        var firstTeamId = Guid.NewGuid();
        var secondTeamId = Guid.NewGuid();

        var connectionRepository =
            new FakeGitHubConnectionRepository(
            [
                CreateConnection(firstTeamId),
                CreateConnection(secondTeamId)
            ]);

        var gitHubSyncService =
            new FakeGitHubSyncService();

        var runner =
            CreateRunner(
                connectionRepository,
                gitHubSyncService);

        // Act
        var result = await runner.RunAsync(
            CancellationToken.None);

        // Assert
        result.Processed.Should().Be(2);
        result.Succeeded.Should().Be(2);
        result.Failed.Should().Be(0);

        gitHubSyncService.SyncedTeamIds.Should()
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
            new FakeGitHubConnectionRepository(
            [
                CreateConnection(firstTeamId),
                CreateConnection(secondTeamId)
            ]);

        var gitHubSyncService =
            new FakeGitHubSyncService
            {
                FailingTeamId = firstTeamId
            };

        var runner =
            CreateRunner(
                connectionRepository,
                gitHubSyncService);

        // Act
        var result = await runner.RunAsync(
            CancellationToken.None);

        // Assert
        gitHubSyncService.SyncedTeamIds.Should()
            .ContainInOrder(
                firstTeamId,
                secondTeamId);

        result.Processed.Should().Be(2);
        result.Succeeded.Should().Be(1);
        result.Failed.Should().Be(1);
    }
    
    [Fact]
    public async Task RunAsync_WhenSyncReturnsNull_ShouldCountAsFailed()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        var connectionRepository =
            new FakeGitHubConnectionRepository(
            [
                CreateConnection(teamId)
            ]);

        var gitHubSyncService =
            new FakeGitHubSyncService
            {
                ReturnNull = true
            };

        var runner =
            CreateRunner(
                connectionRepository,
                gitHubSyncService);

        // Act
        var result = await runner.RunAsync(
            CancellationToken.None);

        // Assert
        result.Processed.Should().Be(1);
        result.Succeeded.Should().Be(0);
        result.Failed.Should().Be(1);
    }
    

    [Fact]
    public async Task RunAsync_WhenSyncTimesOut_ShouldContinueWithOtherConnections()
    {
        // Arrange
        var firstTeamId = Guid.NewGuid();
        var secondTeamId = Guid.NewGuid();

        var connectionRepository =
            new FakeGitHubConnectionRepository(
            [
                CreateConnection(firstTeamId),
                CreateConnection(secondTeamId)
            ]);

        // HttpClient reports its timeout as a TaskCanceledException,
        // not as an HttpRequestException.
        var gitHubSyncService =
            new FakeGitHubSyncService
            {
                FailingTeamId = firstTeamId,
                ExceptionToThrow = new TaskCanceledException("Timeout")
            };

        var runner =
            CreateRunner(
                connectionRepository,
                gitHubSyncService);

        // Act
        var result = await runner.RunAsync(
            CancellationToken.None);

        // Assert
        gitHubSyncService.SyncedTeamIds.Should()
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
            new FakeGitHubConnectionRepository(
            [
                CreateConnection(Guid.NewGuid()),
                CreateConnection(Guid.NewGuid())
            ]);

        var gitHubSyncService =
            new FakeGitHubSyncService();

        var runner =
            CreateRunner(
                connectionRepository,
                gitHubSyncService);

        // Act
        await runner.RunAsync(
            CancellationToken.None);

        // Assert
        gitHubSyncService.ScopeCount.Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_ShouldStopAndPropagateCancellation()
    {
        // Arrange
        var firstTeamId = Guid.NewGuid();

        var connectionRepository =
            new FakeGitHubConnectionRepository(
            [
                CreateConnection(firstTeamId),
                CreateConnection(Guid.NewGuid())
            ]);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        await cancellationTokenSource.CancelAsync();

        var gitHubSyncService =
            new FakeGitHubSyncService
            {
                FailingTeamId = firstTeamId,
                ExceptionToThrow = new OperationCanceledException(
                    cancellationTokenSource.Token)
            };

        var runner =
            CreateRunner(
                connectionRepository,
                gitHubSyncService);

        // Act
        var act = () => runner.RunAsync(
            cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        gitHubSyncService.SyncedTeamIds.Should()
            .Equal(firstTeamId);
    }

    private static GitHubBackgroundSyncRunner CreateRunner(
        IGitHubConnectionRepository connectionRepository,
        FakeGitHubSyncService gitHubSyncService)
    {
        var services = new ServiceCollection();

        services.AddScoped<IGitHubSyncService>(_ =>
        {
            gitHubSyncService.ScopeCount++;

            return gitHubSyncService;
        });

        var serviceProvider = services.BuildServiceProvider();

        return new GitHubBackgroundSyncRunner(
            connectionRepository,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<GitHubBackgroundSyncRunner>.Instance);
    }

    private static GitHubConnection CreateConnection(
        Guid teamId)
    {
        return new GitHubConnection
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Owner = $"org-{teamId:N}",
            OwnerType = GitHubOwnerType.Organization,
            AccessTokenEncrypted = "encrypted-token",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class FakeGitHubConnectionRepository(
        IReadOnlyList<GitHubConnection> connections)
        : IGitHubConnectionRepository
    {
        public Task<IReadOnlyList<GitHubConnection>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(connections);
        }

        public Task<GitHubConnection?> GetByTeamIdAsync(
            Guid teamId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                connections.FirstOrDefault(
                    x => x.TeamId == teamId));
        }

        public Task AddAsync(
            GitHubConnection connection,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(
            GitHubConnection connection,
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

    private sealed class FakeGitHubSyncService
        : IGitHubSyncService
    {
        public List<Guid> SyncedTeamIds { get; } = [];

        public int ScopeCount { get; set; }

        public Guid? FailingTeamId { get; set; }

        public Exception ExceptionToThrow { get; set; } =
            new HttpRequestException("GitHub unavailable");

        public bool ReturnNull { get; set; }
        
        
        public Task<GitHubSyncResponse?> SyncAsync(
            Guid teamId,
            CancellationToken cancellationToken)
        {
            SyncedTeamIds.Add(teamId);

            if (teamId == FailingTeamId)
            {
                throw ExceptionToThrow;
            }
            
            if (ReturnNull)
            {
                return Task.FromResult<GitHubSyncResponse?>(null);
            }

            return Task.FromResult<GitHubSyncResponse?>(
                new GitHubSyncResponse(
                    Synchronized: 1,
                    Created: 1,
                    Updated: 0));
        }
    }
}