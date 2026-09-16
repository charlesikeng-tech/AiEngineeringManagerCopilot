#pragma warning disable OPENAI001

using OpenAI.Responses;

namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public interface IOpenAIClientFactory
{
    ResponsesClient Create(string apiKey);
}

public sealed class OpenAIClientFactory : IOpenAIClientFactory
{
    public ResponsesClient Create(string apiKey)
    {
        return new ResponsesClient(apiKey);
    }
}