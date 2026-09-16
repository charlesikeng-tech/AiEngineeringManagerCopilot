namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed record ChangeFailureRateResult(
    decimal FailureRate,
    int TotalDeployments,
    int FailedDeployments);