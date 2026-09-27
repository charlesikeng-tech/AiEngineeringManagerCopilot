namespace AiEngineeringManagerCopilot.Application.Risks;

public interface IEngineeringRiskService
{
    Task<EngineeringRisksResponse?> GetCurrentAsync(
        Guid teamId,
        CancellationToken cancellationToken);
}