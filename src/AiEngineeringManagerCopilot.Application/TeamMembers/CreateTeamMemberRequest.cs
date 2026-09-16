using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.TeamMembers;

public sealed record CreateTeamMemberRequest(
    string Name,
    string Email,
    TeamMemberRole Role,
    string? ProviderUserId);