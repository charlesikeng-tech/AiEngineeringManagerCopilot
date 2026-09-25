using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentAssertions;
using FluentValidation;

namespace AiEngineeringManagerCopilot.UnitTests.Teams;

public sealed class TeamServiceTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldUseCurrentUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var repository =
            new FakeTeamRepository();

        var currentUser =
            new FakeCurrentUser(userId);

        var service = new TeamService(
            repository,
            currentUser,
            new FakeCreateTeamValidator(),
            new FakeUpdateTeamValidator());

        // Act
        await service.GetByIdAsync(
            teamId,
            CancellationToken.None);

        // Assert
        repository.ReceivedTeamId.Should().Be(teamId);
        repository.ReceivedOwnerUserId.Should().Be(userId);
    }
    
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenTeamDoesNotBelongToCurrentUser()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var repository =
            new FakeTeamRepository
            {
                Team = new Team
                {
                    Id = teamId,
                    OwnerUserId = otherUserId,
                    Name = "Other user's team",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };

        var currentUser =
            new FakeCurrentUser(currentUserId);

        var service = new TeamService(
            repository,
            currentUser,
            new FakeCreateTeamValidator(),
            new FakeUpdateTeamValidator());

        // Act
        var result = await service.GetByIdAsync(
            teamId,
            CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    private sealed class FakeCurrentUser(Guid userId)
        : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class FakeTeamRepository
        : ITeamRepository
    {
        public Guid? ReceivedTeamId { get; private set; }

        public Guid? ReceivedOwnerUserId { get; private set; }
        
        public Team? Team { get; set; }

        public Task<Team?> GetByIdAsync(
            Guid teamId,
            Guid ownerUserId,
            CancellationToken cancellationToken)
        {
            ReceivedTeamId = teamId;
            ReceivedOwnerUserId = ownerUserId;

            var team =
                Team is not null &&
                Team.Id == teamId &&
                Team.OwnerUserId == ownerUserId
                    ? Team
                    : null;

            return Task.FromResult(team);
        }

        public Task<IReadOnlyList<Team>> GetByOwnerAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(
            Team team,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(
            Team team,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeCreateTeamValidator
        : AbstractValidator<CreateTeamRequest>
    {
    }

    private sealed class FakeUpdateTeamValidator
        : AbstractValidator<UpdateTeamRequest>
    {
    }
}