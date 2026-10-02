#pragma warning disable OPENAI001

using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace AiEngineeringManagerCopilot.Api.DependencyInjection;

public static class AIServiceCollectionExtensions
{
    public static IServiceCollection AddAIAnalysisServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<LlmOptions>()
            .Bind(configuration.GetSection(LlmOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<LlmOptions>,
            LlmOptionsValidator>();
        services.AddSingleton<
            ILlmAnalysisParser,
            LlmAnalysisJsonParser>();

        var llmProvider = configuration[
            $"{LlmOptions.SectionName}:Provider"];

        if (string.Equals(
                llmProvider,
                "OpenAI",
                StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(sp =>
            {
                var options = sp
                    .GetRequiredService<IOptions<LlmOptions>>()
                    .Value;

                return new ResponsesClient(options.ApiKey);
            });

            services.AddSingleton<
                IOpenAIResponsesClient,
                OpenAIResponsesClient>();
            services.AddScoped<ILlmProvider, OpenAILlmProvider>();
        }
        else
        {
            services.AddSingleton<ILlmProvider, FakeLlmProvider>();
        }

        services.AddScoped<IAIAnalysisService, AIAnalysisService>();
        services.AddScoped<AIAnalysisPromptBuilder>();
        services.AddScoped<AIEvidenceValidator>();

        return services;
    }
}
