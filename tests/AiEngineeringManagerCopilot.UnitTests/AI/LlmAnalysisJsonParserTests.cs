using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.AI;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class LlmAnalysisJsonParserTests
{
    private readonly LlmAnalysisJsonParser _parser = new();

    [Fact]
    public void Parse_ShouldReturnAnalysis_WhenJsonIsValid()
    {
        const string json = """
        {
          "summary": "The team needs attention.",
          "insights": [
            {
              "category": "Delivery",
              "title": "Slow delivery",
              "description": "Cycle time is high.",
              "impact": "Delivery predictability is affected.",
              "recommendation": "Reduce delivery bottlenecks."
            }
          ],
          "actions": [
            {
              "title": "Reduce cycle time",
              "description": "Identify and remove delivery bottlenecks.",
              "priority": "High"
            }
          ]
        }
        """;

        var result = _parser.Parse(json);

        Assert.Equal(
            "The team needs attention.",
            result.Summary);

        Assert.Single(result.Insights);
        Assert.Single(result.Actions);

        Assert.Equal(
            ActionPriority.High,
            result.Actions[0].Priority);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenContentIsEmpty()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => _parser.Parse(string.Empty));

        Assert.Equal(
            "The LLM returned an empty response.",
            exception.Message);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenJsonIsInvalid()
    {
        const string json = """
        {
          "summary": "Invalid JSON"
        """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => _parser.Parse(json));

        Assert.Equal(
            "The LLM returned an invalid JSON analysis response.",
            exception.Message);

        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenPriorityIsUnknown()
    {
        const string json = """
        {
          "summary": "Analysis",
          "insights": [],
          "actions": [
            {
              "title": "Action",
              "description": "Description",
              "priority": "Urgent"
            }
          ]
        }
        """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => _parser.Parse(json));

        Assert.Equal(
            "The LLM returned an invalid JSON analysis response.",
            exception.Message);
    }
}