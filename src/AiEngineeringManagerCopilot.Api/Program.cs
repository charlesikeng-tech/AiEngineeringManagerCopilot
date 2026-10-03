using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Api.DependencyInjection;
using AiEngineeringManagerCopilot.Api.Endpoints;
using AiEngineeringManagerCopilot.Api.Middleware;
using AiEngineeringManagerCopilot.Infrastructure.Development;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiConfiguration(builder.Configuration)
    .AddAuthenticationServices(builder.Configuration, builder.Environment)
    .AddPersistenceServices(builder.Configuration, builder.Environment)
    .AddApplicationServices()
    .AddIntegrationServices()
    .AddAIAnalysisServices(builder.Configuration)
    .AddBackgroundJobs(builder.Environment);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var initializer = scope.ServiceProvider
        .GetRequiredService<DevelopmentDataInitializer>();

    await initializer.InitializeAsync();
}

app.UseCors(ApiServiceCollectionExtensions.FrontendCorsPolicy);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapTeamEndpoints();
app.MapTeamMemberEndpoints();
app.MapGitHubEndpoints();
app.MapJiraConnectionEndpoints();
app.MapSlackEndpoints();
app.MapMetricsEndpoints();
app.MapHealthEndpoints();
app.MapEngineeringReportEndpoints();
app.MapAIAnalysisEndpoints();
app.MapEngineeringDashboardEndpoints();
app.MapEngineeringRiskEndpoints();
app.MapEngineeringActionEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapPost(
            "/dev/token",
            (DevelopmentJwtTokenGenerator tokenGenerator) =>
            {
                var userId = Guid.Parse(
                    "11111111-1111-1111-1111-111111111111");

                var token = tokenGenerator.Generate(userId);

                return Results.Ok(new
                {
                    accessToken = token,
                    userId
                });
            })
        .WithTags("Development")
        .AllowAnonymous();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks(
        "/health/live",
        new HealthCheckOptions
        {
            Predicate = _ => false
        })
    .AllowAnonymous();

app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = registration =>
                registration.Tags.Contains("ready")
        })
    .AllowAnonymous();

app.Run();

public partial class Program
{
}
