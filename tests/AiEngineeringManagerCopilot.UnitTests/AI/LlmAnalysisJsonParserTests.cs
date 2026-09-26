using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using FluentAssertions;

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
    
    [Fact]
    public void Parse_ShouldParseEvidence()
    {
        const string json = """
                            {
                              "summary": "Engineering delivery is slowing down.",
                              "insights": [],
                              "actions": [],
                              "evidence": [
                                {
                                  "metricType": "CycleTime",
                                  "value": 4.8,
                                  "reason": "Cycle time increased compared with the previous period.",
                                  "confidence": 0.92
                                }
                              ]
                            }
                            """;

        var parser = new LlmAnalysisJsonParser();

        var result = parser.Parse(json);

        result.Evidence.Should().ContainSingle();

        var evidence = result.Evidence.Single();

        evidence.MetricType.Should().Be("CycleTime");
        evidence.Value.Should().Be(4.8m);
        evidence.Reason.Should().Be(
            "Cycle time increased compared with the previous period.");
        evidence.Confidence.Should().Be(0.92m);
    }
    
    [Fact]
    public void Parse_ShouldRejectEvidenceWithInvalidConfidence()
    {
        const string json = """
                            {
                              "summary": "Engineering delivery is slowing down.",
                              "insights": [],
                              "actions": [],
                              "evidence": [
                                {
                                  "metricType": "CycleTime",
                                  "value": 4.8,
                                  "reason": "Cycle time increased.",
                                  "confidence": 1.5
                                }
                              ]
                            }
                            """;

        var parser = new LlmAnalysisJsonParser();

        var act = () => parser.Parse(json);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "LLM evidence confidence must be between 0 and 1.");
    }
}