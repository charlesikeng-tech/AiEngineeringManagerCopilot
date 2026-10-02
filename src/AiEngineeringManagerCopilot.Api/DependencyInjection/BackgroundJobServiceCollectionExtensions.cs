using AiEngineeringManagerCopilot.Application.BackgroundJobs;

namespace AiEngineeringManagerCopilot.Api.DependencyInjection;

public static class BackgroundJobServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddScoped<
            IGitHubBackgroundSyncRunner,
            GitHubBackgroundSyncRunner>();
        services.AddScoped<
            IJiraBackgroundSyncRunner,
            JiraBackgroundSyncRunner>();
        services.AddSingleton<IBackgroundJobDelay, BackgroundJobDelay>();

        if (!environment.IsEnvironment("Test"))
        {
            services.AddHostedService<GitHubSyncBackgroundService>();
            services.AddHostedService<JiraSyncBackgroundService>();
        }

        return services;
    }
}
