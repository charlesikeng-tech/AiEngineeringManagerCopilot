using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;

namespace AiEngineeringManagerCopilot.Api.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";
    public const string AIAnalysisRateLimitPolicy = "AIAnalysis";

    public static IServiceCollection AddApiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter());
        });

        services.AddProblemDetails();
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            const string schemeName = "Bearer";

            options.AddSecurityDefinition(
                schemeName,
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your JWT token."
                });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference(
                            schemeName,
                            document),
                        []
                    }
                });
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.AddPolicy(
                AIAnalysisRateLimitPolicy,
                httpContext =>
                {
                    var userId = httpContext.User.FindFirst(
                        ClaimTypes.NameIdentifier)?.Value;

                    var partitionKey = string.IsNullOrWhiteSpace(userId)
                        ? "anonymous"
                        : userId;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });
        });

        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(
                FrontendCorsPolicy,
                policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins);
                    }

                    policy.AllowAnyHeader().AllowAnyMethod();
                });
        });

        return services;
    }
}
