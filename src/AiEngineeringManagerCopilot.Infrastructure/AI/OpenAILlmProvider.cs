#pragma warning disable OPENAI001
#pragma warning disable SCME0001

using System.Text.Json;
using AiEngineeringManagerCopilot.Application.AI;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public sealed class OpenAILlmProvider(
    IOptions<LlmOptions> options,
    ILlmAnalysisParser parser) : ILlmProvider
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

        var options = new CreateResponseOptions
        {
          Model = _options.Model
        };

        options.InputItems.Add(
          ResponseItem.CreateUserMessageItem(prompt));

        using var schemaDocument =
          JsonDocument.Parse(LlmAnalysisSchema.Create().ToString());

        var format = JsonSerializer.Serialize(
          new
          {
            type = "json_schema",
            name = "engineering_analysis",
            strict = true,
            schema = schemaDocument.RootElement
          });

        options.Patch.Set(
          "$.text.format"u8,
          BinaryData.FromString(format));

        var response = await client.CreateResponseAsync(
          options,
          cancellationToken);

        var json = response.Value.GetOutputText();

        return parser.Parse(json);
    }
}