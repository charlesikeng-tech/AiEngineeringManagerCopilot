using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class LlmOptionsValidatorTests
{
    private readonly LlmOptionsValidator _sut = new();

    [Fact]
    public void Validate_WhenProviderIsFake_ShouldSucceed()
    {
        var options = new LlmOptions
        {
            Provider = "Fake"
        };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenProviderIsOpenAIAndConfigurationIsValid_ShouldSucceed()
    {
        var options = new LlmOptions
        {
            Provider = "OpenAI",
            ApiKey = "test-api-key",
            Model = "test-model"
        };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenOpenAIApiKeyIsMissing_ShouldFail()
    {
        var options = new LlmOptions
        {
            Provider = "OpenAI",
            Model = "test-model"
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(
            "Llm:ApiKey is required when Llm:Provider is OpenAI.");
    }

    [Fact]
    public void Validate_WhenOpenAIModelIsMissing_ShouldFail()
    {
        var options = new LlmOptions
        {
            Provider = "OpenAI",
            ApiKey = "test-api-key"
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(
            "Llm:Model is required when Llm:Provider is OpenAI.");
    }

    [Fact]
    public void Validate_WhenProviderIsUnsupported_ShouldFail()
    {
        var options = new LlmOptions
        {
            Provider = "Unknown"
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Be(
                "Unsupported LLM provider 'Unknown'.");
    }
}