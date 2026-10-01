namespace AiEngineeringManagerCopilot.Application.AI;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    public string Provider { get; init; } = "Fake";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;
}