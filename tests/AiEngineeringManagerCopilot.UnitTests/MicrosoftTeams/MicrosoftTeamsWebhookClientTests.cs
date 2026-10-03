using System.Net;
using System.Text.Json;
using AiEngineeringManagerCopilot.Infrastructure.MicrosoftTeams;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.MicrosoftTeams;

public sealed class MicrosoftTeamsWebhookClientTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.Accepted, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task SendAsync_ShouldPostAdaptiveCardAndReflectDeliveryStatus(
        HttpStatusCode status, bool expected)
    {
        using var handler = new RecordingHandler(status);
        using var http = new HttpClient(handler);
        var client = new MicrosoftTeamsWebhookClient(http);
        var uri = new Uri("https://test.logic.azure.com/workflows/id/triggers/manual/paths/invoke?sig=fake");

        var result = await client.SendAsync(uri, "Report\nTeam: Platform", CancellationToken.None);

        result.Should().Be(expected);
        handler.Method.Should().Be(HttpMethod.Post);
        handler.Uri.Should().Be(uri);
        handler.ContentType.Should().Be("application/json");
        using var json = JsonDocument.Parse(handler.Body!);
        json.RootElement.GetProperty("type").GetString().Should().Be("message");
        var attachment = json.RootElement.GetProperty("attachments")[0];
        attachment.GetProperty("contentType").GetString()
            .Should().Be("application/vnd.microsoft.card.adaptive");
        var card = attachment.GetProperty("content");
        card.GetProperty("type").GetString().Should().Be("AdaptiveCard");
        card.GetProperty("version").GetString().Should().Be("1.2");
        card.GetProperty("body")[0].GetProperty("text").GetString()
            .Should().Be("Report\nTeam: Platform");
        card.GetProperty("body")[0].GetProperty("wrap").GetBoolean().Should().BeTrue();
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status);
        }
    }
}
