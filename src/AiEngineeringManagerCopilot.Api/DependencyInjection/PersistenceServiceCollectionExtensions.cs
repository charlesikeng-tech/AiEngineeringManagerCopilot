using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Infrastructure.Development;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;
using AiEngineeringManagerCopilot.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Api.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString =
                configuration.GetConnectionString("Default");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string 'Default' is not configured.");
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<
            IGitHubConnectionRepository,
            GitHubConnectionRepository>();
        services.AddScoped<IRepositoryRepository, RepositoryRepository>();
        services.AddScoped<IJiraConnectionRepository, JiraConnectionRepository>();
        services.AddScoped<
            ISlackWebhookConnectionRepository,
            SlackWebhookConnectionRepository>();
        services.AddScoped<
            IMicrosoftTeamsWebhookConnectionRepository,
            MicrosoftTeamsWebhookConnectionRepository>();
        services.AddScoped<IJiraWorkItemRepository, JiraWorkItemRepository>();
        services.AddScoped<
            IEngineeringMetricRepository,
            EngineeringMetricRepository>();
        services.AddScoped<IPullRequestRepository, PullRequestRepository>();
        services.AddScoped<IPullRequestReviewRepository, PullRequestReviewRepository>();
        services.AddScoped<IDeploymentRepository, DeploymentRepository>();
        services.AddScoped<
            IEngineeringReportRepository,
            EngineeringReportRepository>();
        services.AddScoped<
            IEngineeringReportInsightRepository,
            EngineeringReportInsightRepository>();
        services.AddScoped<
            IEngineeringActionRepository,
            EngineeringActionRepository>();
        services.AddScoped<IEngineeringRiskRepository, EngineeringRiskRepository>();
        services.AddScoped<IAIAnalysisRepository, AIAnalysisRepository>();
        services.AddScoped<IAIAnalysisInsightRepository, AIAnalysisInsightRepository>();
        services.AddScoped<IAIAnalysisActionRepository, AIAnalysisActionRepository>();
        services.AddScoped<IAIAnalysisEvidenceRepository, AIAnalysisEvidenceRepository>();

        services.AddDataProtection();
        services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();

        if (environment.IsDevelopment())
        {
            services.AddScoped<DevelopmentDataInitializer>();
        }

        return services;
    }
}
