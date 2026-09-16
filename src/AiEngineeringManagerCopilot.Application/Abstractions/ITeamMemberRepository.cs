using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface ITeamMemberRepository
{
    Task<TeamMember?> GetByIdAsync(
        Guid memberId,
        Guid teamId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamMember>> GetByTeamIdAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task AddAsync(
        TeamMember member,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        TeamMember member,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
    
    Task<bool> ExistsByEmailAsync(
        Guid teamId,
        string email,
        Guid? excludingMemberId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByProviderUserIdAsync(
        Guid teamId,
        string providerUserId,
        Guid? excludingMemberId,
        CancellationToken cancellationToken);
}