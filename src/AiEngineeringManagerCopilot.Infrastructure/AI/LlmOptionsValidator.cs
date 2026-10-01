using AiEngineeringManagerCopilot.Application.AI;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public sealed class LlmOptionsValidator
    : IValidateOptions<LlmOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        LlmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            return ValidateOptionsResult.Fail(
                "Llm:Provider is required.");
        }

        if (string.Equals(
                options.Provider,
                "Fake",
                StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Success;
        }

        if (!string.Equals(
                options.Provider,
                "OpenAI",
                StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"Unsupported LLM provider '{options.Provider}'.");
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add(
                "Llm:ApiKey is required when Llm:Provider is OpenAI.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            failures.Add(
                "Llm:Model is required when Llm:Provider is OpenAI.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}