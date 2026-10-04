using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Application.MicrosoftTeams;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Slack;
using AiEngineeringManagerCopilot.Infrastructure.GitHub;
using AiEngineeringManagerCopilot.Infrastructure.MicrosoftTeams;
using AiEngineeringManagerCopilot.Infrastructure.Slack;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.Api.DependencyInjection;

public static class IntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationServices(
        this IServiceCollection services)
    {
        services.AddOptions<ReportNotificationOptions>()
            .BindConfiguration(ReportNotificationOptions.SectionName)
            .Validate(options => ReportNotificationOptions.TryParseBaseUrl(options.FrontendBaseUrl, out _),
                "ReportNotifications:FrontendBaseUrl must be an absolute HTTP(S) URL of at most 1024 characters without credentials, query, or fragment.")
            .ValidateOnStart();
        services.AddScoped(provider => new ReportNotificationFactory(
            provider.GetRequiredService<IOptions<ReportNotificationOptions>>().Value));

        services.AddScoped<IGitHubConnectionService, GitHubConnectionService>();
        services.AddScoped<IGitHubSyncService, GitHubSyncService>();
        services.AddScoped<IJiraConnectionService, JiraConnectionService>();
        services.AddScoped<IJiraSyncService, JiraSyncService>();
        services.AddScoped<SlackWebhookConnectionService>();
        services.AddScoped<ISlackWebhookConnectionService>(
            provider => provider.GetRequiredService<SlackWebhookConnectionService>());
        services.AddScoped<MicrosoftTeamsWebhookConnectionService>();
        services.AddScoped<IMicrosoftTeamsWebhookConnectionService>(
            provider => provider.GetRequiredService<MicrosoftTeamsWebhookConnectionService>());
        services.AddScoped<IEngineeringReportNotifier>(provider =>
            new CompositeEngineeringReportNotifier(
                [
                    provider.GetRequiredService<SlackWebhookConnectionService>(),
                    provider.GetRequiredService<MicrosoftTeamsWebhookConnectionService>()
                ],
                provider.GetRequiredService<ILogger<CompositeEngineeringReportNotifier>>()));

        services.AddSingleton<IRetryDelay, RetryDelay>();

        services.AddHttpClient<IGitHubClient, GitHubClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IJiraClient, JiraClient>();

        services.AddHttpClient<ISlackWebhookClient, SlackWebhookClient>()
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

        // Signed webhook URLs must not be written to the default HTTP request logs.
        services.AddHttpClient<IMicrosoftTeamsWebhookClient, MicrosoftTeamsWebhookClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

        return services;
    }
}
