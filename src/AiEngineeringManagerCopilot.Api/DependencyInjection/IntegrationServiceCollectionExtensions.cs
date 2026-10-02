using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Infrastructure.GitHub;

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

        services.AddSingleton<IRetryDelay, RetryDelay>();

        services.AddHttpClient<IGitHubClient, GitHubClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IJiraClient, JiraClient>();

        return services;
    }
}
