using System.Text.Json;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using FluentAssertions;

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

    [Fact]
    public void Create_ShouldDefineEvidenceSchema()
    {
        var schema = LlmAnalysisSchema.Create();

        using var document =
            JsonDocument.Parse(schema.ToString());

        var root = document.RootElement;

        var evidence = root
            .GetProperty("properties")
            .GetProperty("evidence");

        evidence
            .GetProperty("type")
            .GetString()
            .Should()
            .Be("array");

        var evidenceProperties = evidence
            .GetProperty("items")
            .GetProperty("properties");

        evidenceProperties
            .GetProperty("metricType")
            .GetProperty("type")
            .GetString()
            .Should()
            .Be("string");

        evidenceProperties
            .GetProperty("value")
            .GetProperty("type")
            .GetString()
            .Should()
            .Be("number");

        evidenceProperties
            .GetProperty("reason")
            .GetProperty("type")
            .GetString()
            .Should()
            .Be("string");

        var confidence =
            evidenceProperties.GetProperty("confidence");

        confidence
            .GetProperty("minimum")
            .GetDecimal()
            .Should()
            .Be(0m);

        confidence
            .GetProperty("maximum")
            .GetDecimal()
            .Should()
            .Be(1m);

        root
            .GetProperty("required")
            .EnumerateArray()
            .Select(x => x.GetString())
            .Should()
            .Contain("evidence");
    }
}