namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed record PreviousPeriod(
    DateOnly Start,
    DateOnly End);