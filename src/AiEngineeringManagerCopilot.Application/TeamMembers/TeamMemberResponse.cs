using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.TeamMembers;

public sealed record TeamMemberResponse(
    Guid Id,
    Guid TeamId,
    string Name,
    string Email,
    TeamMemberRole Role,
    string? ProviderUserId,
    DateTimeOffset CreatedAt)
{
    public static TeamMemberResponse FromEntity(
        TeamMember member)
    {
        return new TeamMemberResponse(
            member.Id,
            member.TeamId,
            member.Name,
            member.Email,
            member.Role,
            member.ProviderUserId,
            member.CreatedAt);
    }
}