using System.Text.Json;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.AI;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class LlmAnalysisSchemaTests
{
    [Fact]
    public void Create_ShouldContainAllActionPriorities()
    {
        var schema = LlmAnalysisSchema.Create();

        using var document =
            JsonDocument.Parse(schema.ToString());

        var priorityValues = document.RootElement
            .GetProperty("properties")
            .GetProperty("actions")
            .GetProperty("items")
            .GetProperty("properties")
            .GetProperty("priority")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToList();

        var expectedPriorities = Enum
            .GetNames<ActionPriority>()
            .ToList();

        Assert.Equal(
            expectedPriorities.OrderBy(x => x),
            priorityValues.OrderBy(x => x));
    }
}