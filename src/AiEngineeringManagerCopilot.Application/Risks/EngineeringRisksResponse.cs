namespace AiEngineeringManagerCopilot.Application.Risks;

public sealed record EngineeringRisksResponse(
    Guid TeamId,
    Guid ReportId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<EngineeringRiskResponse> Risks);