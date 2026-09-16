namespace AiEngineeringManagerCopilot.Application.TeamMembers;

public interface ITeamMemberService
{
    Task<TeamMemberResponse?> CreateAsync(
        Guid teamId,
        CreateTeamMemberRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamMemberResponse>?> GetAllAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task<TeamMemberResponse?> GetByIdAsync(
        Guid teamId,
        Guid memberId,
        CancellationToken cancellationToken);

    Task<TeamMemberResponse?> UpdateAsync(
        Guid teamId,
        Guid memberId,
        UpdateTeamMemberRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid teamId,
        Guid memberId,
        CancellationToken cancellationToken);
}