using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Teams;

public sealed record TeamResponse(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt)
{
    public static TeamResponse FromEntity(Team team)
    {
        return new TeamResponse(
            team.Id,
            team.OwnerUserId,
            team.Name,
            team.Description,
            team.CreatedAt);
    }
}