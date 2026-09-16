using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class TeamMemberRepository(
    AppDbContext dbContext)
    : ITeamMemberRepository
{
    public async Task<TeamMember?> GetByIdAsync(
        Guid memberId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TeamMembers
            .FirstOrDefaultAsync(
                x =>
                    x.Id == memberId &&
                    x.TeamId == teamId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TeamMember>> GetByTeamIdAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TeamMembers
            .AsNoTracking()
            .Where(x => x.TeamId == teamId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        TeamMember member,
        CancellationToken cancellationToken)
    {
        await dbContext.TeamMembers.AddAsync(
            member,
            cancellationToken);
    }

    public Task DeleteAsync(
        TeamMember member,
        CancellationToken cancellationToken)
    {
        dbContext.TeamMembers.Remove(member);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(
        Guid teamId,
        string email,
        Guid? excludingMemberId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TeamMembers
            .AnyAsync(
                x =>
                    x.TeamId == teamId &&
                    x.Email == email &&
                    (!excludingMemberId.HasValue ||
                     x.Id != excludingMemberId.Value),
                cancellationToken);
    }

    public async Task<bool> ExistsByProviderUserIdAsync(
        Guid teamId,
        string providerUserId,
        Guid? excludingMemberId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TeamMembers
            .AnyAsync(
                x =>
                    x.TeamId == teamId &&
                    x.ProviderUserId == providerUserId &&
                    (!excludingMemberId.HasValue ||
                     x.Id != excludingMemberId.Value),
                cancellationToken);
    }
}