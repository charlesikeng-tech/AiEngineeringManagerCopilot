using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class LlmProviderContractTests
{
    [Fact]
    public async Task FakeLlmProvider_ShouldReturnValidAnalysis()
    {
        // Arrange
        var sut = new FakeLlmProvider();

        const string prompt = """
                              Analyze the following engineering data.

                              ## Available metrics

                              - CycleTime: 4.8 hours | temporal semantics: ReportingPeriod | Average cycle time during the reporting period.

                              ## Unavailable metrics

                              - PRReviewTime: NoData
                              """;

        // Act
        var result = await sut.AnalyzeAsync(
            prompt,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        result.Summary.Should().NotBeNullOrWhiteSpace();

        result.Insights.Should().NotBeEmpty();

        result.Actions.Should().NotBeEmpty();

        result.Evidence.Should().ContainSingle();

        var evidence = result.Evidence!.Single();

        evidence.MetricType.Should().Be("CycleTime");
        evidence.Value.Should().Be(4.8m);
        evidence.Confidence.Should().BeInRange(0m, 1m);

        sut.LastPrompt.Should().Be(prompt);
    }
}