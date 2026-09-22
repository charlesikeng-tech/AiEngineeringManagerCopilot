using AiEngineeringManagerCopilot.Application.Health;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Health;

public sealed class EngineeringHealthLevelResolverTests
{
    [Theory]
    [InlineData(100, "Excellent")]
    [InlineData(90, "Excellent")]
    [InlineData(89, "Healthy")]
    [InlineData(75, "Healthy")]
    [InlineData(74, "Needs Attention")]
    [InlineData(60, "Needs Attention")]
    [InlineData(59, "At Risk")]
    [InlineData(40, "At Risk")]
    [InlineData(39, "Critical")]
    [InlineData(0, "Critical")]
    public void Resolve_ShouldReturnExpectedHealthLevel(
        int score,
        string expected)
    {
        var result =
            EngineeringHealthLevelResolver.Resolve(
                score,
                100m);

        result.Should().Be(expected);
    }

    [Fact]
    public void Resolve_ShouldReturnNoData_WhenDataCoverageIsZero()
    {
        var result =
            EngineeringHealthLevelResolver.Resolve(
                100,
                0m);

        result.Should().Be("No Data");
    }
}