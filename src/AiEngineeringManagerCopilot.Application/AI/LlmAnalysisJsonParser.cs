using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiEngineeringManagerCopilot.Application.AI;

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

            if (result is null)
            {
                throw new InvalidOperationException(
                    "The LLM returned an invalid analysis response.");
            }

            if (result.Evidence is not null)
            {
                foreach (var evidence in result.SafeEvidence)
                {
                    if (evidence.Confidence is < 0 or > 1)
                    {
                        throw new InvalidOperationException(
                            "LLM evidence confidence must be between 0 and 1.");
                    }
                }
            }

            return result;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The LLM returned an invalid JSON analysis response.",
                exception);
        }
    }
}