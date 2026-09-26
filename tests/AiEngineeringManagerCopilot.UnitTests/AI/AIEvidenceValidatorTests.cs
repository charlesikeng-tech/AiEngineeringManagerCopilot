using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class AIEvidenceValidatorTests
{
    [Fact]
    public void Validate_ShouldAcceptEvidenceMatchingMetric()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 4.8m
        };

        var evidence = new[]
        {
            new LlmEvidenceResult(
                MetricType: "CycleTime",
                Value: 4.8m,
                Reason: "Cycle time indicates a delivery slowdown.",
                Confidence: 0.92m)
        };

        var sut = new AIEvidenceValidator();

        var act = () => sut.Validate(
            evidence,
            metrics);

        act.Should().NotThrow();
    }
    
    [Fact]
    public void Validate_ShouldRejectUnknownMetric()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 4.8m
        };

        var evidence = new[]
        {
            new LlmEvidenceResult(
                MetricType: "ImaginaryMetric",
                Value: 4.8m,
                Reason: "This metric does not exist.",
                Confidence: 0.90m)
        };

        var sut = new AIEvidenceValidator();

        var act = () => sut.Validate(
            evidence,
            metrics);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "AI evidence references unknown metric 'ImaginaryMetric'.");
    }
    
    [Fact]
    public void Validate_ShouldRejectUnavailableMetric()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 4.8m
        };

        var evidence = new[]
        {
            new LlmEvidenceResult(
                MetricType: "DeploymentFrequency",
                Value: 10m,
                Reason: "Deployment frequency indicates delivery activity.",
                Confidence: 0.85m)
        };

        var sut = new AIEvidenceValidator();

        var act = () => sut.Validate(
            evidence,
            metrics);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "AI evidence references unavailable metric 'DeploymentFrequency'.");
    }
    
    [Fact]
    public void Validate_ShouldRejectEvidenceWithIncorrectMetricValue()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 4.8m
        };

        var evidence = new[]
        {
            new LlmEvidenceResult(
                MetricType: "CycleTime",
                Value: 999m,
                Reason: "Cycle time indicates a delivery slowdown.",
                Confidence: 0.92m)
        };

        var sut = new AIEvidenceValidator();

        var act = () => sut.Validate(
            evidence,
            metrics);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "AI evidence value for 'CycleTime' " +
                "does not match the actual metric value.");
    }
    
    [Fact]
    public void Validate_ShouldRejectInvalidConfidence()
    {
        var metrics = new Dictionary<MetricType, decimal>
        {
            [MetricType.CycleTime] = 4.8m
        };

        var evidence = new[]
        {
            new LlmEvidenceResult(
                MetricType: "CycleTime",
                Value: 4.8m,
                Reason: "Cycle time indicates a delivery slowdown.",
                Confidence: 1.5m)
        };

        var sut = new AIEvidenceValidator();

        var act = () => sut.Validate(
            evidence,
            metrics);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "AI evidence confidence must be between 0 and 1.");
    }
}