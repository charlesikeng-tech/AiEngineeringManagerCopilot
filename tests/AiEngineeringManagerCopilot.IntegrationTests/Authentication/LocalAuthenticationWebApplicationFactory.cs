using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.BackgroundJobs;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace AiEngineeringManagerCopilot.IntegrationTests.Authentication;

public sealed class LocalAuthenticationWebApplicationFactory(
    string environmentName = "Test",
    string? setupSecret = LocalAuthenticationWebApplicationFactory.SetupSecret) : WebApplicationFactory<Program>
{
    public const string SetupSecret = "test-only-installation-secret-not-for-deployment-2026";
    public const string JwtKey = "local-auth-test-signing-key-not-for-deployment-2026";
    public static readonly Guid SeedOwnerId = Guid.NewGuid();
    public static readonly Guid SeedTeamId = Guid.NewGuid();
    private readonly string schema = "auth_test_" + Guid.NewGuid().ToString("N");
    private const string ConnectionString = "Host=localhost;Port=5433;Database=ai_engineering_manager_test;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environmentName);
        builder.UseSetting("Authentication:SetupSecret", setupSecret ?? "");
        builder.UseSetting("Jwt:Issuer", "LocalAuthTests");
        builder.UseSetting("Jwt:Audience", "LocalAuthTests");
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("ConnectionStrings:Default", $"{ConnectionString};Search Path={schema}");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:4200");
        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services.Where(x =>
                         x.ImplementationType == typeof(GitHubSyncBackgroundService) ||
                         x.ImplementationType == typeof(JiraSyncBackgroundService)).ToArray())
                services.Remove(descriptor);
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();
            using var command = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection);
            command.ExecuteNonQuery();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            db.Users.Add(new User
            {
                Id = SeedOwnerId, Email = "seed-owner@test.example", Name = "Seed owner", CreatedAt = DateTimeOffset.UtcNow
            });
            db.Teams.Add(new Team
            {
                Id = SeedTeamId, OwnerUserId = SeedOwnerId, Name = "Seed team", CreatedAt = DateTimeOffset.UtcNow
            });
            db.SaveChanges();
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser, HttpCurrentUser>();
        });
    }

    public HttpClient NewClient(bool https = false)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false, BaseAddress = new Uri(https ? "https://localhost" : "http://localhost")
        });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
        client.DefaultRequestHeaders.Add("X-Session-Protection", "1");
        return client;
    }

    public async Task WithDatabase(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }
}
