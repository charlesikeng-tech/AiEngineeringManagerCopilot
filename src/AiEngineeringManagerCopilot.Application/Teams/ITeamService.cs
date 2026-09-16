namespace AiEngineeringManagerCopilot.Application.Teams;

public interface ITeamService
{
    Task<TeamResponse> CreateAsync(
        CreateTeamRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<TeamResponse?> GetByIdAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task<TeamResponse?> UpdateAsync(
        Guid teamId,
        UpdateTeamRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid teamId,
        CancellationToken cancellationToken);
}