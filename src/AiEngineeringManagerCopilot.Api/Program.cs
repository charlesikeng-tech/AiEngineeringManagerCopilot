using System.Text;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Api.Endpoints;
using AiEngineeringManagerCopilot.Api.Middleware;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.BackgroundJobs;
using AiEngineeringManagerCopilot.Application.Dashboard;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Application.GitHub.Validation;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Application.TeamMembers;
using AiEngineeringManagerCopilot.Application.TeamMembers.Validation;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Application.Teams.Validation;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using AiEngineeringManagerCopilot.Infrastructure.Authentication;
using AiEngineeringManagerCopilot.Infrastructure.GitHub;
using AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;
using AiEngineeringManagerCopilot.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidatorsFromAssemblyContaining<
    CreateGitHubConnectionRequestValidator>();

builder.Services.AddDataProtection();

builder.Services.AddProblemDetails();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
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

builder.Services.AddHttpContextAccessor();
var jwtOptions = builder.Configuration
                     .GetSection(JwtOptions.SectionName)
                     .Get<JwtOptions>()
                 ?? throw new InvalidOperationException(
                     "JWT configuration is missing.");

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
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
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.Key)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();


if (builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddScoped<ICurrentUser, DevelopmentCurrentUser>();
}
else
{
    builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
}

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton(jwtOptions);
    builder.Services.AddSingleton<DevelopmentJwtTokenGenerator>();
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Database connection string 'Default' is not configured.");
    }

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
builder.Services.AddScoped<IGitHubConnectionRepository, GitHubConnectionRepository>();
builder.Services.AddScoped<IRepositoryRepository, RepositoryRepository>();
builder.Services.AddScoped<IJiraConnectionRepository, JiraConnectionRepository>();
builder.Services.AddScoped<IJiraWorkItemRepository, JiraWorkItemRepository>();
builder.Services.AddScoped<IEngineeringMetricRepository, EngineeringMetricRepository>();
builder.Services.AddScoped<IPullRequestRepository, PullRequestRepository>();
builder.Services.AddScoped<IPullRequestReviewRepository, PullRequestReviewRepository>();
builder.Services.AddScoped<IDeploymentRepository, DeploymentRepository>();
builder.Services.AddScoped<IEngineeringReportRepository, EngineeringReportRepository>();
builder.Services.AddScoped<IEngineeringReportInsightRepository, EngineeringReportInsightRepository>();
builder.Services.AddScoped<IEngineeringActionRepository, EngineeringActionRepository>();
builder.Services.AddScoped<IEngineeringRiskRepository, EngineeringRiskRepository>();
builder.Services.AddScoped<IAIAnalysisRepository, AIAnalysisRepository>();
builder.Services.AddScoped<IAIAnalysisInsightRepository, AIAnalysisInsightRepository>();
builder.Services.AddScoped<IAIAnalysisActionRepository, AIAnalysisActionRepository>();

builder.Services.AddScoped<IValidator<CreateTeamRequest>, CreateTeamRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateTeamRequest>, UpdateTeamRequestValidator>();
builder.Services.AddScoped<IValidator<CreateTeamMemberRequest>, CreateTeamMemberRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateTeamMemberRequest>, UpdateTeamMemberRequestValidator>();

builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ITeamMemberService, TeamMemberService>();
builder.Services.AddScoped<IGitHubConnectionService, GitHubConnectionService>();
builder.Services.AddScoped<IGitHubSyncService, GitHubSyncService>();
builder.Services.AddScoped<IJiraConnectionService, JiraConnectionService>();
builder.Services.AddScoped<IJiraSyncService, JiraSyncService>();
builder.Services.AddScoped<IEngineeringMetricsService, EngineeringMetricsService>();
builder.Services.AddScoped<IEngineeringHealthScoreService, EngineeringHealthScoreService>();
builder.Services.AddScoped<IEngineeringReportService, EngineeringReportService>();
builder.Services.AddScoped<IEngineeringDashboardService, EngineeringDashboardService>();

builder.Services.AddScoped<ICycleTimeCalculator, CycleTimeCalculator>();
builder.Services.AddScoped<IPRReviewTimeCalculator, PRReviewTimeCalculator>();
builder.Services.AddScoped<IDeploymentFrequencyCalculator, DeploymentFrequencyCalculator>();
builder.Services.AddScoped<IChangeFailureRateCalculator, ChangeFailureRateCalculator>();
builder.Services.AddScoped<ILeadTimeCalculator, LeadTimeCalculator>();
builder.Services.AddScoped<IOpenPullRequestsCalculator, OpenPullRequestsCalculator>();
builder.Services.AddScoped<IMergedPullRequestsCalculator, MergedPullRequestsCalculator>();
builder.Services.AddScoped<IBlockedItemsCalculator, BlockedItemsCalculator>();
builder.Services.AddScoped<IEngineeringHealthScoreCalculator, EngineeringHealthScoreCalculator>();
builder.Services.AddScoped<IEngineeringInsightGenerator, EngineeringInsightGenerator>();
builder.Services.AddScoped<IEngineeringActionGenerator, EngineeringActionGenerator>();
builder.Services.AddScoped< IEngineeringMetricScoreCalculator, EngineeringMetricScoreCalculator>();

builder.Services.AddScoped<IEngineeringRiskDetector, EngineeringRiskDetector>();

builder.Services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();
builder.Services.AddSingleton<PreviousPeriodCalculator>();
builder.Services.AddSingleton<MetricTrendCalculator>();
builder.Services.AddSingleton<MetricTrendBuilder>();

builder.Services.AddSingleton<EngineeringTrendSignalDetector>();
builder.Services.AddSingleton<EngineeringTrendInsightGenerator>();
builder.Services.AddSingleton<EngineeringTrendInsightService>();

var llmProvider = builder.Configuration["Llm:Provider"];

if (string.Equals(llmProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<ILlmProvider, OpenAILlmProvider>();
}
else
{
    builder.Services.AddSingleton<ILlmProvider, FakeLlmProvider>();
}
builder.Services.AddScoped< IAIAnalysisService, AIAnalysisService>();
builder.Services.AddScoped<AIAnalysisPromptBuilder>();
builder.Services.AddScoped<AIEvidenceValidator>();
builder.Services.AddScoped<IAIAnalysisActionRepository, AIAnalysisActionRepository>();
builder.Services.AddScoped<IAIAnalysisEvidenceRepository, AIAnalysisEvidenceRepository>();
builder.Services
    .AddOptions<LlmOptions>()
    .Bind(builder.Configuration.GetSection("Llm"));
builder.Services.AddSingleton<
    ILlmAnalysisParser,
    LlmAnalysisJsonParser>();

builder.Services.AddSingleton<IRetryDelay, RetryDelay>();
builder.Services.AddScoped<IGitHubBackgroundSyncRunner, GitHubBackgroundSyncRunner>();
builder.Services.AddScoped<IJiraBackgroundSyncRunner, JiraBackgroundSyncRunner>();
builder.Services.AddSingleton<IBackgroundJobDelay, BackgroundJobDelay>();

if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddHostedService<
        GitHubSyncBackgroundService>();

    builder.Services.AddHostedService<
        JiraSyncBackgroundService>();
}
builder.Services.AddHttpClient<IGitHubClient, GitHubClient>(
    client =>
    {
        client.BaseAddress =
            new Uri("https://api.github.com/");

        client.Timeout =
            TimeSpan.FromSeconds(30);
    });


builder.Services.AddHttpClient<IJiraClient, JiraClient>();
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await DevelopmentDataSeeder.SeedAsync(dbContext);
}

app.MapTeamEndpoints();
app.MapTeamMemberEndpoints();
app.MapGitHubEndpoints();
app.MapJiraConnectionEndpoints();
app.MapMetricsEndpoints();
app.MapHealthEndpoints();
app.MapEngineeringReportEndpoints();
app.MapAIAnalysisEndpoints();
app.MapEngineeringDashboardEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapPost(
            "/dev/token",
            (
                DevelopmentJwtTokenGenerator tokenGenerator) =>
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
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "Healthy"
    }));

app.Run();

public partial class Program
{
}