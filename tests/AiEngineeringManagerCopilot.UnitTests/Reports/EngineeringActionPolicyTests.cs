using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Reports;

public sealed class EngineeringActionPolicyTests
{
    [Theory]
    [InlineData(RiskCategory.Quality, ActionPriority.Critical)]
    [InlineData(RiskCategory.Reliability, ActionPriority.Critical)]
    [InlineData(RiskCategory.Delivery, ActionPriority.High)]
    [InlineData(RiskCategory.Review, ActionPriority.High)]
    [InlineData(RiskCategory.Process, ActionPriority.Medium)]
    [InlineData(RiskCategory.Ownership, ActionPriority.Medium)]
    [InlineData(RiskCategory.TechnicalDebt, ActionPriority.Medium)]
    public void GetPriority_ShouldReturnExpectedPriority(
        RiskCategory category,
        ActionPriority expectedPriority)
    {
        var result = EngineeringActionPolicy.GetPriority(category);

        result.Should().Be(expectedPriority);
    }
}