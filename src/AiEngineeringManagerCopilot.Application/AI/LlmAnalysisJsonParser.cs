using System.Text.Json;
using System.Text.Json.Serialization;
using AiEngineeringManagerCopilot.Application.AI;

namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public sealed class LlmAnalysisJsonParser : ILlmAnalysisParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public LlmAnalysisResult Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "The LLM returned an empty response.");
        }

        try
        {
            var result = JsonSerializer.Deserialize<LlmAnalysisResult>(
                content,
                SerializerOptions);

            return result
                   ?? throw new InvalidOperationException(
                       "The LLM returned an invalid analysis response.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The LLM returned an invalid JSON analysis response.",
                exception);
        }
    }
}