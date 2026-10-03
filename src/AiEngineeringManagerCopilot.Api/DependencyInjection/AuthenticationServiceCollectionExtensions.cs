using System.Text;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AiEngineeringManagerCopilot.Api.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<JwtOptions>,
            JwtOptionsValidator>();

        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration is missing.");

        services.AddIdentityCore<IdentityUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequiredUniqueChars = 4;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
        }).AddUserStore<PasswordValidationUserStore>();
        services.AddRateLimiter(options => options.AddPolicy("LocalAuthentication", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0, AutoReplenishment = true
                })));

        services.AddAuthentication("BearerOrSession")
            .AddPolicyScheme("BearerOrSession", null, options =>
                options.ForwardDefaultSelector = context =>
                    context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : LocalSessionAuthenticationHandler.Scheme)
            .AddScheme<AuthenticationSchemeOptions, LocalSessionAuthenticationHandler>(
                LocalSessionAuthenticationHandler.Scheme, _ => { })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
            });

        services.AddAuthorization(options => options.AddPolicy("LocalAdministrator", policy =>
        {
            policy.AddAuthenticationSchemes("BearerOrSession");
            policy.RequireAuthenticatedUser();
            policy.RequireRole(LocalSessionAuthenticationHandler.AdministratorRole);
            policy.RequireAssertion(context =>
                context.User.Identity?.AuthenticationType == LocalSessionAuthenticationHandler.Scheme);
        }));

        if (environment.IsDevelopment())
        {
            services.AddSingleton(jwtOptions);
            services.AddSingleton<DevelopmentJwtTokenGenerator>();
        }

        if (environment.IsEnvironment("Test"))
        {
            services.AddScoped<
                ICurrentUser,
                DevelopmentCurrentUser>();
        }
        else
        {
            services.AddScoped<
                ICurrentUser,
                HttpCurrentUser>();
        }

        return services;
    }
}
