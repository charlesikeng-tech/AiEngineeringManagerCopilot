using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Authentication;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private static readonly object DatabaseInitializationLock = new();

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        builder.UseSetting(
            "Jwt:Issuer",
            "AiEngineeringManagerCopilot.Tests");

        builder.UseSetting(
            "Jwt:Audience",
            "AiEngineeringManagerCopilot.Tests");

        builder.UseSetting(
            "Jwt:Key",
            "ai-engineering-manager-copilot-test-key-2026-secure-enough-for-tests");

        builder.UseSetting(
            "ConnectionStrings:Default",
            "Host=localhost;Port=5433;Database=ai_engineering_manager_test;Username=postgres;Password=postgres");

        builder.ConfigureServices(services =>
        {
            lock (DatabaseInitializationLock)
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
            }
            
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthHandler.SchemeName;

                    options.DefaultChallengeScheme =
                        TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            services.RemoveAll<ICurrentUser>();

            services.AddScoped<
                ICurrentUser,
                HttpCurrentUser>();

            services.RemoveAll<IGitHubClient>();

            services.AddSingleton<FakeGitHubClient>();

            services.AddSingleton<IGitHubClient>(
                provider =>
                    provider.GetRequiredService<FakeGitHubClient>());

            services.RemoveAll<IJiraClient>();

            services.AddSingleton<FakeJiraClient>();

            services.AddSingleton<IJiraClient>(
                provider =>
                    provider.GetRequiredService<FakeJiraClient>());
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