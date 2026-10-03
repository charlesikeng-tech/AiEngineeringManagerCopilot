using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentAssertions;
using FluentValidation;

namespace AiEngineeringManagerCopilot.UnitTests.Teams;

public sealed class TeamServiceTests
{
    [Fact]
    public async Task GetPageAsync_ShouldScopePaginationToCurrentUserAndMapResults()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeTeamRepository
        {
            Team = new Team { Id = Guid.NewGuid(), OwnerUserId = userId, Name = "Platform" }
        };
        var service = new TeamService(
            repository, new FakeCurrentUser(userId), new FakeCreateTeamValidator(), new FakeUpdateTeamValidator());
        var result = await service.GetPageAsync(new GetTeamsPageRequest(2, 20, "Platform"), CancellationToken.None);
        repository.ReceivedOwnerUserId.Should().Be(userId);
        repository.ReceivedPageNumber.Should().Be(2);
        repository.ReceivedPageSize.Should().Be(20);
        repository.ReceivedSearch.Should().Be("Platform");
        result.TotalCount.Should().Be(25);
        result.Items.Should().ContainSingle().Which.Name.Should().Be("Platform");
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(20);
    }

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

        public Task<PagedResult<Team>> GetPageByOwnerAsync(
            Guid ownerUserId, int pageNumber, int pageSize, string? search,
            CancellationToken cancellationToken)
        {
            ReceivedOwnerUserId = ownerUserId;
            ReceivedPageNumber = pageNumber;
            ReceivedPageSize = pageSize;
            ReceivedSearch = search;
            return Task.FromResult(new PagedResult<Team>(
                Team is null ? [] : [Team], 25, pageNumber, pageSize));
        }

        public int ReceivedPageNumber { get; private set; }
        public int ReceivedPageSize { get; private set; }
        public string? ReceivedSearch { get; private set; }

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