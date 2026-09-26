using AiEngineeringManagerCopilot.Domain.Entities;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class AIAnalysisEvidenceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void SetConfidence_ShouldAcceptValueBetweenZeroAndOne(
        decimal confidence)
    {
        var evidence = new AIAnalysisEvidence();

        evidence.SetConfidence(confidence);

        evidence.Confidence.Should().Be(confidence);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void SetConfidence_ShouldRejectValueOutsideRange(
        decimal confidence)
    {
        var evidence = new AIAnalysisEvidence();

        var act = () =>
            evidence.SetConfidence(confidence);

        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }
}