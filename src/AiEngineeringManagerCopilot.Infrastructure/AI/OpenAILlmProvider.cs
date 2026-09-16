#pragma warning disable OPENAI001

using System.Text.Json;
using AiEngineeringManagerCopilot.Application.AI;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public sealed class OpenAILlmProvider(
    IOptions<LlmOptions> options) : ILlmProvider
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

        var client = new ResponsesClient(_options.ApiKey);

        var response = await client.CreateResponseAsync(
            _options.Model,
            prompt,
            cancellationToken: cancellationToken);

        var json = response.Value.GetOutputText();

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                "The LLM returned an empty response.");
        }

        var result = JsonSerializer.Deserialize<LlmAnalysisResult>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result is null)
        {
            throw new InvalidOperationException(
                "The LLM returned an invalid analysis response.");
        }

        return result;
    }
}