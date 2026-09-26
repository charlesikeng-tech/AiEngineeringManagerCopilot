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
        var provider = new FakeLlmProvider();

        var result = await provider.AnalyzeAsync(
            "Analyze this engineering team.",
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Summary.Should().NotBeNullOrWhiteSpace();

        result.Insights.Should().NotBeEmpty();
        result.Actions.Should().NotBeEmpty();

        result.Actions[0].Priority
            .Should()
            .Be(ActionPriority.High);
        
        result.Evidence.Should().ContainSingle();

        var evidence = result.Evidence.Single();

        evidence.MetricType.Should().Be("CycleTime");
        evidence.Value.Should().Be(4.8m);
        evidence.Confidence.Should().Be(0.92m);
        evidence.Reason.Should().Be(
            "Cycle time indicates a potential delivery slowdown.");
    }
}