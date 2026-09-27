using System.Net.Http.Json;

namespace AiEngineeringManagerCopilot.IntegrationTests.Helpers;

public static class HttpContentExtensions
{
    public static Task<T?> ReadApiJsonAsync<T>(
        this HttpContent content,
        CancellationToken cancellationToken = default)
    {
        return content.ReadFromJsonAsync<T>(
            IntegrationTestJsonOptions.Default,
            cancellationToken);
    }
}