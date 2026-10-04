using System.Net;
using System.Text;
using System.Text.Json;
using AiEngineeringManagerCopilot.Application.Actions;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.MicrosoftTeams;
using AiEngineeringManagerCopilot.Infrastructure.Slack;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Reports;

public sealed class ReportNotificationTests
{
    private static readonly Guid TeamId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ReportId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Factory_ShouldRankPrioritiesAndBuildTeamScopedDeepLink()
    {
        var report = CreateReport();
        var notification = Factory().Create(TeamId, "Platform", report);
        notification.ReportUrl.AbsoluteUri.Should()
            .Be($"https://portal.example.com/copilot/reports/{ReportId}?teamId={TeamId}");
        notification.Risks.Select(item => item.Priority).Should().Equal("Critical", "High", "Medium");
        notification.Actions.Should().HaveCount(3);
        notification.Actions[0].Priority.Should().Be("Critical");
        notification.Actions.Should().NotContain(item => item.Title == "Completed critical action");
        notification.RiskCount.Should().Be(4);
        notification.ActionCount.Should().Be(5);
        notification.Coverage.Should().Be("95%");
        notification.FallbackText.Should().Contain("Health score: 80/100 (Healthy)");
    }

    [Fact]
    public void Factory_ShouldBoundLongContentWithoutSplittingSurrogates()
    {
        var report = CreateReport() with
        {
            ExecutiveSummary = new string('x', 396) + char.ConvertFromUtf32(0x1F680) + new string('x', 1000),
            Risks = CreateReport().Risks.Select(risk => risk with
            {
                Title = new string('x', 2000), Description = new string('x', 2000)
            }).ToArray()
        };
        var notification = Factory().Create(TeamId, new string('x', 2000), report);
        notification.TeamName.Length.Should().BeLessThanOrEqualTo(120);
        notification.ExecutiveSummary.Length.Should().BeLessThanOrEqualTo(400);
        notification.Risks.Should().OnlyContain(item => item.Title.Length <= 80 && item.Description.Length <= 160);
        notification.ExecutiveSummary.Should().EndWith("...");
        notification.ExecutiveSummary.Should().Be(new string('x', 396) + "...");
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:pass@portal.example.com")]
    [InlineData("https://portal.example.com?token=fake")]
    [InlineData("https://portal.example.com#report")]
    [InlineData("")]
    public void Options_ShouldRejectInvalidFrontendAddresses(string value) =>
        ReportNotificationOptions.TryParseBaseUrl(value, out _).Should().BeFalse();

    [Fact]
    public void Factory_ShouldRejectMismatchedTeamRatherThanCreateWrongLink()
    {
        var create = () => Factory().Create(Guid.NewGuid(), "Wrong team", CreateReport());
        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Slack_ShouldSendBlockKitSummaryAndButtonWithoutMentionInjection()
    {
        using var handler = new CaptureHandler();
        using var http = new HttpClient(handler);
        var notification = Factory().Create(TeamId, "<!channel>", CreateReport());
        var client = new SlackWebhookClient(http);
        (await client.SendReportAsync(
            new Uri("https://hooks.slack.com/services/fake/token/test"), notification, CancellationToken.None))
            .Should().BeTrue();
        using var json = JsonDocument.Parse(handler.Body!);
        var root = json.RootElement;
        root.GetProperty("text").GetString().Should().Contain("&lt;!channel&gt;").And.NotContain("<!channel>");
        root.GetProperty("parse").GetString().Should().Be("none");
        root.GetProperty("unfurl_links").GetBoolean().Should().BeFalse();
        var blocks = root.GetProperty("blocks").EnumerateArray().ToArray();
        blocks.Length.Should().BeLessThanOrEqualTo(50);
        blocks[0].GetProperty("type").GetString().Should().Be("header");
        var button = blocks.Single(block => block.GetProperty("type").GetString() == "actions")
            .GetProperty("elements")[0];
        button.GetProperty("style").GetString().Should().Be("primary");
        button.GetProperty("url").GetString().Should().Be(notification.ReportUrl.AbsoluteUri);
        foreach (var block in blocks.Where(block => block.TryGetProperty("text", out _)))
        {
            block.GetProperty("text").GetProperty("text").GetString()!.Length.Should().BeLessThanOrEqualTo(3000);
        }
        handler.Body.Should().Contain("Executive summary").And.Contain("Top risks").And.Contain("Priority actions");
    }

    [Fact]
    public async Task Teams_ShouldSendColoredAdaptiveCardAndOpenUrlButton()
    {
        using var handler = new CaptureHandler();
        using var http = new HttpClient(handler);
        var notification = Factory().Create(TeamId, "[Platform](https://untrusted.example)", CreateReport());
        var client = new MicrosoftTeamsWebhookClient(http);
        (await client.SendReportAsync(
            new Uri("https://test.logic.azure.com/workflows/test/triggers/manual/paths/invoke?sig=fake"),
            notification, CancellationToken.None)).Should().BeTrue();
        using var json = JsonDocument.Parse(handler.Body!);
        var card = json.RootElement.GetProperty("attachments")[0].GetProperty("content");
        card.GetProperty("type").GetString().Should().Be("AdaptiveCard");
        card.GetProperty("version").GetString().Should().Be("1.2");
        var body = card.GetProperty("body");
        body[1].GetProperty("color").GetString().Should().Be("Good");
        body[2].GetProperty("facts")[0].GetProperty("value").GetString().Should().StartWith("\\[Platform\\]");
        card.GetProperty("actions")[0].GetProperty("type").GetString().Should().Be("Action.OpenUrl");
        card.GetProperty("actions")[0].GetProperty("url").GetString().Should().Be(notification.ReportUrl.AbsoluteUri);
        Encoding.UTF8.GetByteCount(handler.Body!).Should().BeLessThan(28 * 1024);
    }

    [Fact]
    public async Task Clients_ShouldSurfaceRejectedRichMessages()
    {
        using var handler = new CaptureHandler(HttpStatusCode.BadRequest);
        using var http = new HttpClient(handler);
        var notification = Factory().Create(TeamId, "Platform", CreateReport());
        var url = new Uri("https://example.com/fake");
        (await new SlackWebhookClient(http).SendReportAsync(url, notification, CancellationToken.None)).Should().BeFalse();
        (await new MicrosoftTeamsWebhookClient(http).SendReportAsync(url, notification, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task MaximumContent_ShouldRemainWithinPlatformPayloadLimits()
    {
        var text = new string('<', 4000);
        var report = CreateReport() with
        {
            ExecutiveSummary = text, HealthLevel = text,
            Risks = CreateReport().Risks.Select(item => item with { Title = text, Description = text }).ToArray(),
            Actions = CreateReport().Actions.Select(item => item with { Title = text, Description = text }).ToArray()
        };
        var prefix = "https://portal.example.com/";
        var factory = new ReportNotificationFactory(new ReportNotificationOptions
        {
            FrontendBaseUrl = prefix + new string('&', 1024 - prefix.Length)
        });
        var notification = factory.Create(TeamId, text, report);
        using var handler = new CaptureHandler();
        using var http = new HttpClient(handler);
        await new MicrosoftTeamsWebhookClient(http).SendReportAsync(new Uri("https://example.com"), notification, CancellationToken.None);
        Encoding.UTF8.GetByteCount(handler.Body!).Should().BeLessThan(28 * 1024);
        await new SlackWebhookClient(http).SendReportAsync(new Uri("https://example.com"), notification, CancellationToken.None);
        using var json = JsonDocument.Parse(handler.Body!);
        foreach (var block in json.RootElement.GetProperty("blocks").EnumerateArray())
        {
            if (block.TryGetProperty("text", out var blockText))
                blockText.GetProperty("text").GetString()!.Length.Should().BeLessThanOrEqualTo(3000);
        }
    }

    private static ReportNotificationFactory Factory() => new(new ReportNotificationOptions
    {
        FrontendBaseUrl = "https://portal.example.com/copilot/"
    });

    private static EngineeringReportResponse CreateReport()
    {
        var risks = new[] { RiskSeverity.Low, RiskSeverity.Critical, RiskSeverity.High, RiskSeverity.Medium }
            .Select(severity => new EngineeringReportRiskResponse(
                Guid.NewGuid(), ReportId, severity, RiskCategory.Delivery,
                $"{severity} risk", "Evidence-based risk description", "Recommendation", DateTimeOffset.UtcNow)).ToArray();
        var actions = new[] { ActionPriority.Low, ActionPriority.Critical, ActionPriority.High, ActionPriority.Medium }
            .Select(priority => new EngineeringActionResponse(
                Guid.NewGuid(), ReportId, null, $"{priority} action", "Concrete recommended action",
                priority, null, null, "Todo", DateTimeOffset.UtcNow)).ToList();
        actions.Add(new EngineeringActionResponse(Guid.NewGuid(), ReportId, null,
            "Completed critical action", "Completed", ActionPriority.Critical, null, null, "Done", DateTimeOffset.UtcNow));
        return new EngineeringReportResponse(
            ReportId, TeamId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            "Delivery is healthy, but review the prioritized risks.", 80, "Healthy", 95, DateTimeOffset.UtcNow,
            [], [], actions, risks, []);
    }

    private sealed class CaptureHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status);
        }
    }
}
