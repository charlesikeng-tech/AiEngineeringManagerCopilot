using System.Text.Json.Serialization;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Api.DependencyInjection;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Actions;
using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.BackgroundJobs;
using AiEngineeringManagerCopilot.Application.Dashboard;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.MicrosoftTeams;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Application.TeamMembers;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using AiEngineeringManagerCopilot.Infrastructure.Authentication;
using AiEngineeringManagerCopilot.Infrastructure.Development;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.UnitTests.DependencyInjection;

public sealed class ServiceRegistrationTests
{
    [Theory]
    [InlineData("Development", "Fake")]
    [InlineData("Test", "Fake")]
    [InlineData("Production", "Fake")]
    [InlineData("Development", "OpenAI")]
    [InlineData("Test", "OpenAI")]
    [InlineData("Production", "openai")]
    public async Task Registration_ShouldResolveApplicationServices(
        string environmentName,
        string provider)
    {
        var builder = CreateBuilder(environmentName, provider);

        await using var app = builder.Build();

        app.Services.GetRequiredService<IStartupValidator>().Validate();

        using var scope = app.Services.CreateScope();

        var serviceTypes = new[]
        {
            typeof(ITeamService),
            typeof(ITeamMemberService),
            typeof(IGitHubConnectionService),
            typeof(IGitHubSyncService),
            typeof(IJiraConnectionService),
            typeof(IJiraSyncService),
            typeof(IMicrosoftTeamsWebhookConnectionService),
            typeof(IMicrosoftTeamsWebhookClient),
            typeof(IMicrosoftTeamsWebhookConnectionRepository),
            typeof(IValidator<CreateMicrosoftTeamsWebhookRequest>),
            typeof(IEngineeringMetricsService),
            typeof(IEngineeringHealthScoreService),
            typeof(IEngineeringReportService),
            typeof(IEngineeringDashboardService),
            typeof(IEngineeringRiskService),
            typeof(IEngineeringActionService),
            typeof(IAIAnalysisService),
            typeof(IGitHubBackgroundSyncRunner),
            typeof(IJiraBackgroundSyncRunner),
            typeof(IValidator<CreateTeamRequest>),
            typeof(IValidator<UpdateTeamRequest>),
            typeof(IValidator<CreateTeamMemberRequest>),
            typeof(IValidator<UpdateTeamMemberRequest>)
        };

        foreach (var serviceType in serviceTypes)
        {
            scope.ServiceProvider.GetRequiredService(serviceType)
                .Should().NotBeNull();
        }

        var llmProvider = scope.ServiceProvider
            .GetRequiredService<ILlmProvider>();

        if (string.Equals(provider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            llmProvider.Should().BeOfType<FakeLlmProvider>();
        }
        else
        {
            llmProvider.Should().BeOfType<OpenAILlmProvider>();
        }
    }

    [Theory]
    [InlineData("Development", true, false)]
    [InlineData("Test", false, true)]
    [InlineData("Production", false, false)]
    public async Task Registration_ShouldPreserveEnvironmentSpecificServices(
        string environmentName,
        bool isDevelopment,
        bool isTest)
    {
        var builder = CreateBuilder(environmentName);

        builder.Services.Any(descriptor =>
                descriptor.ServiceType == typeof(DevelopmentDataInitializer))
            .Should().Be(isDevelopment);
        builder.Services.Any(descriptor =>
                descriptor.ServiceType == typeof(DevelopmentJwtTokenGenerator))
            .Should().Be(isDevelopment);
        builder.Services.Any(descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(GitHubSyncBackgroundService))
            .Should().Be(!isTest);
        builder.Services.Any(descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(JiraSyncBackgroundService))
            .Should().Be(!isTest);

        await using var app = builder.Build();

        using var scope = app.Services.CreateScope();

        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        currentUser.GetType().Should().Be(
            isTest ? typeof(DevelopmentCurrentUser) : typeof(HttpCurrentUser));

        if (isDevelopment)
        {
            scope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>()
                .Should().NotBeNull();
            app.Services.GetRequiredService<DevelopmentJwtTokenGenerator>()
                .Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Registration_ShouldPreserveScopedAndSingletonLifetimes()
    {
        await using var app = CreateBuilder("Test").Build();
        using var firstScope = app.Services.CreateScope();
        using var secondScope = app.Services.CreateScope();

        firstScope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Should().BeSameAs(
                firstScope.ServiceProvider.GetRequiredService<AppDbContext>());
        firstScope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Should().NotBeSameAs(
                secondScope.ServiceProvider.GetRequiredService<AppDbContext>());
        firstScope.ServiceProvider.GetRequiredService<IAIAnalysisService>()
            .Should().NotBeSameAs(
                secondScope.ServiceProvider.GetRequiredService<IAIAnalysisService>());
        firstScope.ServiceProvider.GetRequiredService<ILlmProvider>()
            .Should().BeSameAs(
                secondScope.ServiceProvider.GetRequiredService<ILlmProvider>());
        firstScope.ServiceProvider.GetRequiredService<MetricTrendBuilder>()
            .Should().BeSameAs(
                secondScope.ServiceProvider.GetRequiredService<MetricTrendBuilder>());
    }

    [Fact]
    public async Task Registration_ShouldPreserveApiAndAuthenticationConfiguration()
    {
        await using var app = CreateBuilder("Test").Build();

        var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value;
        jsonOptions.SerializerOptions.Converters
            .Should().Contain(converter => converter is JsonStringEnumConverter);

        var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(
            ApiServiceCollectionExtensions.FrontendCorsPolicy);
        policy.Should().NotBeNull();
        policy!.Origins.Should().ContainSingle().Which.Should().Be("http://localhost:4200");
        policy.AllowAnyHeader.Should().BeTrue();
        policy.AllowAnyMethod.Should().BeTrue();
        policy.AllowAnyOrigin.Should().BeFalse();

        var jwtOptions = app.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        jwtOptions.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidIssuer.Should().Be("test-issuer");
        jwtOptions.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidAudience.Should().Be("test-audience");
        jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData("Jwt:Key", "too-short")]
    [InlineData("Llm:Provider", "Unknown")]
    [InlineData("Llm:ApiKey", "")]
    [InlineData("Llm:Model", "")]
    public async Task Registration_ShouldRejectInvalidOptionsAtStartup(
        string configurationKey,
        string value)
    {
        var builder = CreateBuilder("Production", "OpenAI");
        builder.Configuration[configurationKey] = value;

        await using var app = builder.Build();

        var validate = () => app.Services
            .GetRequiredService<IStartupValidator>()
            .Validate();

        validate.Should().Throw<OptionsValidationException>();
    }

    private static WebApplicationBuilder CreateBuilder(
        string environmentName,
        string provider = "Fake")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName,
            ApplicationName = typeof(Program).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });

        builder.Configuration["Jwt:Issuer"] = "test-issuer";
        builder.Configuration["Jwt:Audience"] = "test-audience";
        builder.Configuration["Jwt:Key"] = "01234567890123456789012345678901";
        builder.Configuration["Llm:Provider"] = provider;
        builder.Configuration["Llm:ApiKey"] = "test-api-key";
        builder.Configuration["Llm:Model"] = "test-model";
        builder.Configuration["ConnectionStrings:Default"] =
            "Host=localhost;Database=registration_tests";
        builder.Configuration["Cors:AllowedOrigins:0"] = "http://localhost:4200";

        builder.Services
            .AddApiConfiguration(builder.Configuration)
            .AddAuthenticationServices(builder.Configuration, builder.Environment)
            .AddPersistenceServices(builder.Configuration, builder.Environment)
            .AddApplicationServices()
            .AddIntegrationServices()
            .AddAIAnalysisServices(builder.Configuration)
            .AddBackgroundJobs(builder.Environment);

        return builder;
    }
}
