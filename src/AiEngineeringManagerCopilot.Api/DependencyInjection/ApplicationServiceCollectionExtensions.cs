using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Actions;
using AiEngineeringManagerCopilot.Application.Dashboard;
using AiEngineeringManagerCopilot.Application.GitHub.Validation;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Application.TeamMembers;
using AiEngineeringManagerCopilot.Application.TeamMembers.Validation;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Application.Teams.Validation;
using FluentValidation;

namespace AiEngineeringManagerCopilot.Api.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<
            CreateGitHubConnectionRequestValidator>();

        services.AddScoped<
            IValidator<CreateTeamRequest>,
            CreateTeamRequestValidator>();
        services.AddScoped<
            IValidator<UpdateTeamRequest>,
            UpdateTeamRequestValidator>();
        services.AddScoped<
            IValidator<CreateTeamMemberRequest>,
            CreateTeamMemberRequestValidator>();
        services.AddScoped<
            IValidator<UpdateTeamMemberRequest>,
            UpdateTeamMemberRequestValidator>();

        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<ITeamMemberService, TeamMemberService>();
        services.AddScoped<
            IEngineeringMetricsService,
            EngineeringMetricsService>();
        services.AddScoped<
            IEngineeringHealthScoreService,
            EngineeringHealthScoreService>();
        services.AddScoped<
            IEngineeringReportService,
            EngineeringReportService>();
        services.AddScoped<
            IEngineeringDashboardService,
            EngineeringDashboardService>();
        services.AddScoped<IEngineeringRiskService, EngineeringRiskService>();
        services.AddScoped<IEngineeringActionService, EngineeringActionService>();

        services.AddScoped<ICycleTimeCalculator, CycleTimeCalculator>();
        services.AddScoped<IPRReviewTimeCalculator, PRReviewTimeCalculator>();
        services.AddScoped<
            IDeploymentFrequencyCalculator,
            DeploymentFrequencyCalculator>();
        services.AddScoped<
            IChangeFailureRateCalculator,
            ChangeFailureRateCalculator>();
        services.AddScoped<ILeadTimeCalculator, LeadTimeCalculator>();
        services.AddScoped<IOpenPullRequestsCalculator, OpenPullRequestsCalculator>();
        services.AddScoped<
            IMergedPullRequestsCalculator,
            MergedPullRequestsCalculator>();
        services.AddScoped<IBlockedItemsCalculator, BlockedItemsCalculator>();
        services.AddScoped<
            IEngineeringHealthScoreCalculator,
            EngineeringHealthScoreCalculator>();
        services.AddScoped<
            IEngineeringInsightGenerator,
            EngineeringInsightGenerator>();
        services.AddScoped<
            IEngineeringActionGenerator,
            EngineeringActionGenerator>();
        services.AddScoped<
            IEngineeringMetricScoreCalculator,
            EngineeringMetricScoreCalculator>();
        services.AddScoped<IEngineeringRiskDetector, EngineeringRiskDetector>();

        services.AddSingleton<PreviousPeriodCalculator>();
        services.AddSingleton<MetricTrendCalculator>();
        services.AddSingleton<MetricTrendBuilder>();
        services.AddSingleton<EngineeringTrendSignalDetector>();
        services.AddSingleton<EngineeringTrendInsightGenerator>();
        services.AddSingleton<EngineeringTrendInsightService>();

        return services;
    }
}
