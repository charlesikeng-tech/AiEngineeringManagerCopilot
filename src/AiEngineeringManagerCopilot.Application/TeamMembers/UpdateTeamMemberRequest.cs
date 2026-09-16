using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.TeamMembers;

public sealed record UpdateTeamMemberRequest(
    string Name,
    string Email,
    TeamMemberRole Role,
    string? ProviderUserId);