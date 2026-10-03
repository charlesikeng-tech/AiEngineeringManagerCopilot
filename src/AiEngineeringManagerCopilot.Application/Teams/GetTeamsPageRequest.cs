namespace AiEngineeringManagerCopilot.Application.Teams;

public sealed record GetTeamsPageRequest(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null);
