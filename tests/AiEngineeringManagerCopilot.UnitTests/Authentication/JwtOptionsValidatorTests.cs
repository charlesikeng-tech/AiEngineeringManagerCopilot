using AiEngineeringManagerCopilot.Api.Authentication;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Authentication;

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _sut = new();

    [Fact]
    public void Validate_WhenConfigurationIsValid_ShouldSucceed()
    {
        var options = CreateOptions();

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIssuerIsMissing_ShouldFail()
    {
        var options = CreateOptions(
            issuer: string.Empty);

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(
            "Jwt:Issuer is required.");
    }

    [Fact]
    public void Validate_WhenAudienceIsMissing_ShouldFail()
    {
        var options = CreateOptions(
            audience: string.Empty);

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(
            "Jwt:Audience is required.");
    }

    [Fact]
    public void Validate_WhenKeyIsMissing_ShouldFail()
    {
        var options = CreateOptions(
            key: string.Empty);

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(
            "Jwt:Key is required.");
    }

    [Fact]
    public void Validate_WhenKeyIsTooShort_ShouldFail()
    {
        var options = CreateOptions(
            key: "too-short");

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(
            "Jwt:Key must be at least 32 characters.");
    }

    private static JwtOptions CreateOptions(
        string issuer = "test-issuer",
        string audience = "test-audience",
        string key = "01234567890123456789012345678901")
    {
        return new JwtOptions
        {
            Issuer = issuer,
            Audience = audience,
            Key = key
        };
    }
}