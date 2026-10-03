using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Slack;
using AiEngineeringManagerCopilot.Infrastructure.GitHub;
using AiEngineeringManagerCopilot.Infrastructure.Slack;

namespace AiEngineeringManagerCopilot.Api.DependencyInjection;

public static class IntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IGitHubConnectionService, GitHubConnectionService>();
        services.AddScoped<IGitHubSyncService, GitHubSyncService>();
        services.AddScoped<IJiraConnectionService, JiraConnectionService>();
        services.AddScoped<IJiraSyncService, JiraSyncService>();
        services.AddScoped<
            ISlackWebhookConnectionService,
            SlackWebhookConnectionService>();
        services.AddScoped<
            IEngineeringReportNotifier,
            SlackWebhookConnectionService>();

        services.AddSingleton<IRetryDelay, RetryDelay>();

        services.AddHttpClient<IGitHubClient, GitHubClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IJiraClient, JiraClient>();

        services.AddHttpClient<ISlackWebhookClient, SlackWebhookClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

        return services;
    }
}
