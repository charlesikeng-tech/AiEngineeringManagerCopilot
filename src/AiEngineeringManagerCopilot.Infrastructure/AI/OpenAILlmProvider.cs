using System.Text.Json;
using AiEngineeringManagerCopilot.Application.AI;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public sealed class OpenAILlmProvider(
    IOptions<LlmOptions> options,
    ILlmAnalysisParser parser,
    IOpenAIResponsesClient client)
    : ILlmProvider
{
    private readonly LlmOptions _options = options.Value;

    public async Task<LlmAnalysisResult> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "LLM API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException(
                "LLM model is not configured.");
        }

        using var schemaDocument =
            JsonDocument.Parse(
                LlmAnalysisSchema.Create().ToString());

        var format = JsonSerializer.Serialize(
            new
            {
                type = "json_schema",
                name = "engineering_analysis",
                strict = true,
                schema = schemaDocument.RootElement
            });

        var json = await client.CreateResponseAsync(
            _options.Model,
            prompt,
            format,
            cancellationToken);

        return parser.Parse(json);
    }
}