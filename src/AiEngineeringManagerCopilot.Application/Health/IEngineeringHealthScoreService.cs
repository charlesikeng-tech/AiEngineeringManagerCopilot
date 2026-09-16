namespace AiEngineeringManagerCopilot.Application.Health;

public interface IEngineeringHealthScoreService
{
    Task<EngineeringHealthScoreResponse?> CalculateAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken);
}