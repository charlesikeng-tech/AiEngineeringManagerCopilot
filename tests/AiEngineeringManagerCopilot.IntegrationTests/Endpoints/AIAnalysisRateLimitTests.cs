using System.Net;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.IntegrationTests.Endpoints;

public sealed class AIAnalysisRateLimitTests
    : IClassFixture<AIAnalysisRateLimitWebApplicationFactory>
{
    private readonly AIAnalysisRateLimitWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AIAnalysisRateLimitTests(
        AIAnalysisRateLimitWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AnalyzeReport_ShouldLimitRequestsPerUserAndLeaveGetUnrestricted()
    {
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        for (var requestNumber = 0; requestNumber < 5; requestNumber++)
        {
            var response = await _client.PostAsync(
                $"/teams/{teamId}/reports/{reportId}/analyze",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        var rejectedResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports/{reportId}/analyze",
            null);

        rejectedResponse.StatusCode.Should()
            .Be(HttpStatusCode.TooManyRequests);
        rejectedResponse.Headers.RetryAfter.Should().NotBeNull();
        rejectedResponse.Headers.RetryAfter!.Delta.Should()
            .BeGreaterThan(TimeSpan.Zero);

        using var otherUserClient = _factory.CreateClient();
        otherUserClient.DefaultRequestHeaders.Add(
            "X-Test-UserId",
            Guid.NewGuid().ToString());
        var otherUserResponse = await otherUserClient.PostAsync(
            $"/teams/{teamId}/reports/{reportId}/analyze",
            null);

        otherUserResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var getResponse = await _client.GetAsync(
            $"/teams/{teamId}/reports/{reportId}/analysis");

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public sealed class AIAnalysisRateLimitWebApplicationFactory
    : CustomWebApplicationFactory
{
    protected override int AIAnalysisRequestLimit => 5;
}
