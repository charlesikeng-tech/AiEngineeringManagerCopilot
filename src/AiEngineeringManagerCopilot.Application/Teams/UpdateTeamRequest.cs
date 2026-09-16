namespace AiEngineeringManagerCopilot.Application.Teams;

public sealed record UpdateTeamRequest(
    string Name,
    string? Description);