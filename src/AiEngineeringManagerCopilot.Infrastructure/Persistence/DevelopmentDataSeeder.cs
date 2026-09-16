using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

        var exists = await dbContext.Users
            .AnyAsync(x => x.Id == userId, cancellationToken);

        if (exists)
        {
            return;
        }

        var user = new User
        {
            Id = userId,
            Email = "dev@ai-engineering-manager.local",
            Name = "Development User",
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}