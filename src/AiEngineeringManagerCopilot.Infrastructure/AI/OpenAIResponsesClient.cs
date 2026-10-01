#pragma warning disable OPENAI001
#pragma warning disable SCME0001

using OpenAI.Responses;

namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public sealed class OpenAIResponsesClient(
    ResponsesClient client)
    : IOpenAIResponsesClient
{
    public async Task<string> CreateResponseAsync(
        string model,
        string prompt,
        string responseFormat,
        CancellationToken cancellationToken)
    {
        var options = new CreateResponseOptions
        {
            Model = model
        };

        options.InputItems.Add(
            ResponseItem.CreateUserMessageItem(prompt));

        options.Patch.Set(
            "$.text.format"u8,
            BinaryData.FromString(responseFormat));

        var response = await client.CreateResponseAsync(
            options,
            cancellationToken);

        return response.Value.GetOutputText();
    }
}