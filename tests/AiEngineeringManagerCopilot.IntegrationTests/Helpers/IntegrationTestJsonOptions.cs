using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiEngineeringManagerCopilot.IntegrationTests.Helpers;

public static class IntegrationTestJsonOptions
{
    public static JsonSerializerOptions Default { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(
            JsonSerializerDefaults.Web);

        options.Converters.Add(
            new JsonStringEnumConverter());

        return options;
    }
}