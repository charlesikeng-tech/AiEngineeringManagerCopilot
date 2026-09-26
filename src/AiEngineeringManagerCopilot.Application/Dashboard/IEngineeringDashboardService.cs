namespace AiEngineeringManagerCopilot.Application.Dashboard;

public interface IEngineeringDashboardService
{
    Task<EngineeringDashboardResponse?> GetAsync(
        Guid teamId,
        CancellationToken cancellationToken);
}