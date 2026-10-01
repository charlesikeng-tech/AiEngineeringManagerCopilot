namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public interface IOpenAIResponsesClient
{
    Task<string> CreateResponseAsync(
        string model,
        string prompt,
        string responseFormat,
        CancellationToken cancellationToken);
}