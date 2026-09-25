using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.BackgroundJobs;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentAssertions;

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
            new GitHubBackgroundSyncRunner(
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
            new GitHubBackgroundSyncRunner(
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
            new GitHubBackgroundSyncRunner(
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
    

    private static GitHubConnection CreateConnection(
        Guid teamId)
    {
        return new GitHubConnection
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Organization = $"org-{teamId:N}",
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

        public Guid? FailingTeamId { get; set; }

        public bool ReturnNull { get; set; }
        
        
        public Task<GitHubSyncResponse?> SyncAsync(
            Guid teamId,
            CancellationToken cancellationToken)
        {
            SyncedTeamIds.Add(teamId);

            if (teamId == FailingTeamId)
            {
                throw new HttpRequestException(
                    "GitHub unavailable");
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