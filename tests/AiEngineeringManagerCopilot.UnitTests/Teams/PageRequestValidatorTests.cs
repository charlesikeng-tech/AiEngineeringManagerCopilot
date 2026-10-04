using AiEngineeringManagerCopilot.Application.Common;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Teams;

public sealed class PageRequestValidatorTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    [InlineData(int.MinValue, int.MinValue)]
    public void Validate_ShouldRejectInvalidBounds(int pageNumber, int pageSize) =>
        new PageRequestValidator().Validate(new PageRequest(pageNumber, pageSize)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(1, 10, 0)]
    [InlineData(1, 100, 0)]
    [InlineData(2, 10, 10)]
    [InlineData(int.MaxValue, 1, int.MaxValue - 1)]
    public void Validate_ShouldAcceptSafeOffsets(int pageNumber, int pageSize, int offset)
    {
        var request = new PageRequest(pageNumber, pageSize);
        new PageRequestValidator().Validate(request).IsValid.Should().BeTrue();
        request.Offset.Should().Be(offset);
    }
}
