namespace AiEngineeringManagerCopilot.Application.Actions;

public sealed record EngineeringActionsResponse(
    Guid TeamId,
    Guid ReportId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<EngineeringActionResponse> Actions);