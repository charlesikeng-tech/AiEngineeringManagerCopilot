using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.UseSetting(
            "ConnectionStrings:Default",
            "Host=localhost;Port=5433;Database=ai_engineering_manager_test;Username=postgres;Password=postgres");

        builder.ConfigureServices(services =>
        {
            using var serviceProvider =
                services.BuildServiceProvider();

            using var scope =
                serviceProvider.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            dbContext.Database.Migrate();

            SeedTestUser(dbContext);
            
            services.RemoveAll<IGitHubClient>();

            services.AddSingleton<FakeGitHubClient>();

            services.AddSingleton<IGitHubClient>(
                provider =>
                    provider.GetRequiredService<FakeGitHubClient>());
        });
    }

    private static void SeedTestUser(
        AppDbContext dbContext)
    {
        var userId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

        if (dbContext.Users.Any(x => x.Id == userId))
        {
            return;
        }

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                Email = "test@ai-engineering-manager.local",
                Name = "Test User",
                CreatedAt = DateTimeOffset.UtcNow
            });

        dbContext.SaveChanges();
    }
}