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
        var options = CreateOptions(
            apiKey: string.Empty,
            model: "test-model");

        var client = new FakeOpenAIResponsesClient();

        var provider = CreateProvider(
            options,
            client);

        var act = async () =>
            await provider.AnalyzeAsync(
                "Analyze this engineering team.",
                CancellationToken.None);

        var exception = await act.Should()
            .ThrowAsync<InvalidOperationException>();

        exception.Which.Message.Should()
            .Be("LLM API key is not configured.");

        client.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldThrow_WhenModelIsMissing()
    {
        var options = CreateOptions(
            apiKey: "test-api-key",
            model: string.Empty);

        var client = new FakeOpenAIResponsesClient();

        var provider = CreateProvider(
            options,
            client);

        var act = async () =>
            await provider.AnalyzeAsync(
                "Analyze this engineering team.",
                CancellationToken.None);

        var exception = await act.Should()
            .ThrowAsync<InvalidOperationException>();

        exception.Which.Message.Should()
            .Be("LLM model is not configured.");

        client.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldPassModelAndPromptToClient()
    {
        var options = CreateOptions(
            apiKey: "test-api-key",
            model: "test-model");

        var client = new FakeOpenAIResponsesClient
        {
            Response = CreateValidAnalysisJson()
        };

        var provider = CreateProvider(
            options,
            client);

        await provider.AnalyzeAsync(
            "Analyze this engineering team.",
            CancellationToken.None);

        client.CallCount.Should().Be(1);

        client.Model.Should().Be("test-model");

        client.Prompt.Should()
            .Be("Analyze this engineering team.");
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldPassStrictJsonSchemaToClient()
    {
        var options = CreateOptions(
            apiKey: "test-api-key",
            model: "test-model");

        var client = new FakeOpenAIResponsesClient
        {
            Response = CreateValidAnalysisJson()
        };

        var provider = CreateProvider(
            options,
            client);

        await provider.AnalyzeAsync(
            "Analyze this engineering team.",
            CancellationToken.None);

        client.ResponseFormat.Should()
            .NotBeNullOrWhiteSpace();

        client.ResponseFormat.Should()
            .Contain("\"type\":\"json_schema\"");

        client.ResponseFormat.Should()
            .Contain("\"name\":\"engineering_analysis\"");

        client.ResponseFormat.Should()
            .Contain("\"strict\":true");

        client.ResponseFormat.Should()
            .Contain("\"schema\"");
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldPropagateCancellationToken()
    {
        var options = CreateOptions(
            apiKey: "test-api-key",
            model: "test-model");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var client = new FakeOpenAIResponsesClient
        {
            Response = CreateValidAnalysisJson()
        };

        var provider = CreateProvider(
            options,
            client);

        await provider.AnalyzeAsync(
            "Analyze this engineering team.",
            cancellationTokenSource.Token);

        client.CancellationToken.Should()
            .Be(cancellationTokenSource.Token);
    }

    private static OpenAILlmProvider CreateProvider(
        IOptions<LlmOptions> options,
        IOpenAIResponsesClient client)
    {
        return new OpenAILlmProvider(
            options,
            new LlmAnalysisJsonParser(),
            client);
    }

    private static IOptions<LlmOptions> CreateOptions(
        string apiKey,
        string model)
    {
        return Options.Create(
            new LlmOptions
            {
                Provider = "OpenAI",
                ApiKey = apiKey,
                Model = model
            });
    }

    private static string CreateValidAnalysisJson()
    {
        return
            """
            {
              "summary": "Engineering health is stable.",
              "risks": [],
              "insights": [],
              "actions": []
            }
            """;
    }

    private sealed class FakeOpenAIResponsesClient
        : IOpenAIResponsesClient
    {
        public string Response { get; init; } =
            CreateValidAnalysisJson();

        public int CallCount { get; private set; }

        public string? Model { get; private set; }

        public string? Prompt { get; private set; }

        public string? ResponseFormat { get; private set; }

        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        public Task<string> CreateResponseAsync(
            string model,
            string prompt,
            string responseFormat,
            CancellationToken cancellationToken)
        {
            CallCount++;

            Model = model;
            Prompt = prompt;
            ResponseFormat = responseFormat;
            CancellationToken = cancellationToken;

            return Task.FromResult(Response);
        }
    }
}