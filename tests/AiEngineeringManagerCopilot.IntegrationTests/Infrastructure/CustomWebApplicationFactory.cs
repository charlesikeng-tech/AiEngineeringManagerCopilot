using System.Globalization;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Application.Slack;
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
using Npgsql;

namespace AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=ai_engineering_manager_test;Username=postgres;Password=postgres";

    // Arbitrary application-wide key for pg_advisory_lock.
    private const long DatabaseInitializationLockKey = 7_420_260_920;

    protected virtual int AIAnalysisRequestLimit => int.MaxValue;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting(
            "RateLimiting:AIAnalysis:PermitLimit",
            AIAnalysisRequestLimit.ToString(CultureInfo.InvariantCulture));
        
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
            ConnectionString);
        
        builder.UseSetting(
            "Cors:AllowedOrigins:0",
            "http://localhost:4200");
        
        builder.ConfigureServices(services =>
        {
            // The unit and integration test assemblies run in parallel
            // processes against the same database, so an in-process lock
            // is not enough to serialize migrations: use a PostgreSQL lock.
            using (var lockConnection = new NpgsqlConnection(ConnectionString))
            {
                lockConnection.Open();

                ExecuteLockCommand(lockConnection, "pg_advisory_lock");

                try
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
                finally
                {
                    // Pooled connections keep their session open on close,
                    // so the lock must be released explicitly.
                    ExecuteLockCommand(lockConnection, "pg_advisory_unlock");
                }
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

            services.RemoveAll<ISlackWebhookClient>();

            services.AddSingleton<FakeSlackWebhookClient>();

            services.AddSingleton<ISlackWebhookClient>(
                provider =>
                    provider.GetRequiredService<FakeSlackWebhookClient>());

            services.RemoveAll<IMicrosoftTeamsWebhookClient>();
            services.AddSingleton<FakeMicrosoftTeamsWebhookClient>();
            services.AddSingleton<IMicrosoftTeamsWebhookClient>(
                provider => provider.GetRequiredService<FakeMicrosoftTeamsWebhookClient>());
        });
    }

    private static void ExecuteLockCommand(
        NpgsqlConnection connection,
        string function)
    {
        using var command = new NpgsqlCommand(
            $"SELECT {function}({DatabaseInitializationLockKey})",
            connection);

        command.ExecuteNonQuery();
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