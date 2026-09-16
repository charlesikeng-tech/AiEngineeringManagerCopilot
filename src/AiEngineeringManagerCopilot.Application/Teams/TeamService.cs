using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.Teams;

public sealed class TeamService(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IValidator<CreateTeamRequest> createTeamValidator,
    IValidator<UpdateTeamRequest> updateTeamValidator) : ITeamService
{
    public async Task<TeamResponse> CreateAsync(
        CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        await RequestValidator.ValidateAndThrowAsync(
            request,
            createTeamValidator,
            cancellationToken);

        var name = request.Name.Trim();

        var description = request.Description?.Trim();

        var team = new Team
        {
            Id = Guid.NewGuid(),
            OwnerUserId = currentUser.UserId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(description)
                ? null
                : description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await teamRepository.AddAsync(
            team,
            cancellationToken);

        await teamRepository.SaveChangesAsync(
            cancellationToken);

        return TeamResponse.FromEntity(team);
    }

    public async Task<IReadOnlyList<TeamResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var teams = await teamRepository.GetByOwnerAsync(
            currentUser.UserId,
            cancellationToken);

        return teams
            .Select(TeamResponse.FromEntity)
            .ToList();
    }

    public async Task<TeamResponse?> GetByIdAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        return team is null
            ? null
            : TeamResponse.FromEntity(team);
    }

    public async Task<TeamResponse?> UpdateAsync(
        Guid teamId,
        UpdateTeamRequest request,
        CancellationToken cancellationToken)
    {
        await RequestValidator.ValidateAndThrowAsync(
            request,
            updateTeamValidator,
            cancellationToken);

        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var name = request.Name.Trim();
        var description = request.Description?.Trim();

        team.Name = name;
        team.Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description;

        await teamRepository.SaveChangesAsync(
            cancellationToken);

        return TeamResponse.FromEntity(team);
    }

    public async Task<bool> DeleteAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return false;
        }

        await teamRepository.DeleteAsync(
            team,
            cancellationToken);

        await teamRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}