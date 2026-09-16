namespace AiEngineeringManagerCopilot.Application.Teams;

public sealed record CreateTeamRequest(
    string Name,
    string? Description);