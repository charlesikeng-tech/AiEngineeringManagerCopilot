using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Application.Teams.Validation;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Teams;

public sealed class GetTeamsPageRequestValidatorTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public void Validate_ShouldRejectInvalidBounds(int pageNumber, int pageSize) =>
        new GetTeamsPageRequestValidator().Validate(new GetTeamsPageRequest(pageNumber, pageSize))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ShouldAcceptDefaultsAndRejectOverlongSearch()
    {
        var validator = new GetTeamsPageRequestValidator();
        validator.Validate(new GetTeamsPageRequest()).IsValid.Should().BeTrue();
        validator.Validate(new GetTeamsPageRequest(Search: new string('x', 201))).IsValid.Should().BeFalse();
    }
}
