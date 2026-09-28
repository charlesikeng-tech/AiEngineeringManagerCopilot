namespace AiEngineeringManagerCopilot.Application.Actions;

public interface IEngineeringActionService
{
    Task<EngineeringActionsResponse?> GetCurrentAsync(
        Guid teamId,
        CancellationToken cancellationToken);
    
    Task<EngineeringActionResponse?> UpdateAsync(
        Guid teamId,
        Guid actionId,
        UpdateEngineeringActionRequest request,
        CancellationToken cancellationToken);
}