using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    private static readonly Guid DevelopmentUserId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    private static readonly Guid DevelopmentTeamId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await SeedUserAsync(
            dbContext,
            cancellationToken);

        await SeedTeamAsync(
            dbContext,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static async Task SeedUserAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users
            .AnyAsync(
                x => x.Id == DevelopmentUserId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.Users.Add(
            new User
            {
                Id = DevelopmentUserId,
                Email = "dev@ai-engineering-manager.local",
                Name = "Development User",
                CreatedAt = DateTimeOffset.UtcNow
            });
    }

    private static async Task SeedTeamAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Teams
            .AnyAsync(
                x => x.Id == DevelopmentTeamId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.Teams.Add(
            new Team
            {
                Id = DevelopmentTeamId,
                OwnerUserId = DevelopmentUserId,
                Name = "Development Team",
                Description = "Development environment team",
                CreatedAt = DateTimeOffset.UtcNow
            });
    }
}