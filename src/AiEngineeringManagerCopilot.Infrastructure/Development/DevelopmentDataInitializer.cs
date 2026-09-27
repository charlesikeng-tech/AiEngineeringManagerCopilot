using AiEngineeringManagerCopilot.Infrastructure.Persistence;

namespace AiEngineeringManagerCopilot.Infrastructure.Development;

public sealed class DevelopmentDataInitializer(
    AppDbContext dbContext)
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await DevelopmentDataSeeder.SeedAsync(
            dbContext,
            cancellationToken);
    }
}