using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Domain.Entities;
using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.TeamMembers;

public sealed class TeamMemberService(
    ITeamRepository teamRepository,
    ITeamMemberRepository teamMemberRepository,
    ICurrentUser currentUser,
    IValidator<CreateTeamMemberRequest> createValidator,
    IValidator<UpdateTeamMemberRequest> updateValidator)
    : ITeamMemberService
{
    public async Task<TeamMemberResponse?> CreateAsync(
        Guid teamId,
        CreateTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        await RequestValidator.ValidateAndThrowAsync(
            request,
            createValidator,
            cancellationToken);

        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var email = request.Email.Trim();

        if (await teamMemberRepository.ExistsByEmailAsync(
                teamId,
                email,
                null,
                cancellationToken))
        {
            throw new ConflictException(
                $"A team member with email '{email}' already exists.");
        }

        var providerUserId =
            string.IsNullOrWhiteSpace(request.ProviderUserId)
                ? null
                : request.ProviderUserId.Trim();

        if (providerUserId is not null &&
            await teamMemberRepository.ExistsByProviderUserIdAsync(
                teamId,
                providerUserId,
                null,
                cancellationToken))
        {
            throw new ConflictException(
                $"A team member with provider user id '{providerUserId}' already exists.");
        }

        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Name = request.Name.Trim(),
            Email = email,
            Role = request.Role,
            ProviderUserId = providerUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await teamMemberRepository.AddAsync(
            member,
            cancellationToken);

        await teamMemberRepository.SaveChangesAsync(
            cancellationToken);

        return TeamMemberResponse.FromEntity(member);
    }

    public async Task<IReadOnlyList<TeamMemberResponse>?> GetAllAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var members = await teamMemberRepository.GetByTeamIdAsync(
            teamId,
            cancellationToken);

        return members
            .Select(TeamMemberResponse.FromEntity)
            .ToList();
    }

    public async Task<TeamMemberResponse?> GetByIdAsync(
        Guid teamId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var member = await teamMemberRepository.GetByIdAsync(
            memberId,
            teamId,
            cancellationToken);

        return member is null
            ? null
            : TeamMemberResponse.FromEntity(member);
    }

    public async Task<TeamMemberResponse?> UpdateAsync(
        Guid teamId,
        Guid memberId,
        UpdateTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        await RequestValidator.ValidateAndThrowAsync(
            request,
            updateValidator,
            cancellationToken);

        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var member = await teamMemberRepository.GetByIdAsync(
            memberId,
            teamId,
            cancellationToken);

        if (member is null)
        {
            return null;
        }

        var email = request.Email.Trim();

        if (await teamMemberRepository.ExistsByEmailAsync(
                teamId,
                email,
                memberId,
                cancellationToken))
        {
            throw new ConflictException(
                $"A team member with email '{email}' already exists.");
        }

        var providerUserId =
            string.IsNullOrWhiteSpace(request.ProviderUserId)
                ? null
                : request.ProviderUserId.Trim();

        if (providerUserId is not null &&
            await teamMemberRepository.ExistsByProviderUserIdAsync(
                teamId,
                providerUserId,
                memberId,
                cancellationToken))
        {
            throw new ConflictException(
                $"A team member with provider user id '{providerUserId}' already exists.");
        }

        member.Name = request.Name.Trim();
        member.Email = email;
        member.Role = request.Role;
        member.ProviderUserId = providerUserId;

        await teamMemberRepository.SaveChangesAsync(
            cancellationToken);

        return TeamMemberResponse.FromEntity(member);
    }

    public async Task<bool> DeleteAsync(
        Guid teamId,
        Guid memberId,
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

        var member = await teamMemberRepository.GetByIdAsync(
            memberId,
            teamId,
            cancellationToken);

        if (member is null)
        {
            return false;
        }

        await teamMemberRepository.DeleteAsync(
            member,
            cancellationToken);

        await teamMemberRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}