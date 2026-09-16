namespace AiEngineeringManagerCopilot.Application.AI;

public sealed class LlmOptions
{
    public string Provider { get; set; } = "Fake";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}