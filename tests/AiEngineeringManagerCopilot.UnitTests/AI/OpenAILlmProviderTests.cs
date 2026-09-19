using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class OpenAILlmProviderTests
{
    [Fact]
    public async Task AnalyzeAsync_ShouldThrow_WhenApiKeyIsMissing()
    {
        var options = Options.Create(
            new LlmOptions
            {
                Provider = "OpenAI",
                ApiKey = string.Empty,
                Model = "test-model"
            });

        var provider = new OpenAILlmProvider(
            options,
            new LlmAnalysisJsonParser());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.AnalyzeAsync(
                "Analyze this engineering team.",
                CancellationToken.None));

        Assert.Equal(
            "LLM API key is not configured.",
            exception.Message);
    }
}