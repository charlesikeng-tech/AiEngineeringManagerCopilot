using AiEngineeringManagerCopilot.Application.MicrosoftTeams;
using AiEngineeringManagerCopilot.Application.MicrosoftTeams.Validation;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.MicrosoftTeams;

public sealed class MicrosoftTeamsWebhookAddressTests
{
    [Theory]
    [InlineData("https://test.environment.api.powerplatform.com/powerautomate/automations/direct/workflows/id/triggers/manual/paths/invoke?api-version=1&sig=fake")]
    [InlineData("https://prod-01.westeurope.logic.azure.com/workflows/id/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=fake")]
    [InlineData("https://outlook.office.com/webhook/id/IncomingWebhook/fake/channel")]
    [InlineData("https://tenant.webhook.office.com/webhookb2/id/IncomingWebhook/fake/channel")]
    public void TryParse_ShouldAcceptSupportedWebhookUrls(string url)
    {
        MicrosoftTeamsWebhookAddress.TryParse($" {url} ", out var uri).Should().BeTrue();
        uri!.AbsoluteUri.Should().Be(url);
        new CreateMicrosoftTeamsWebhookRequestValidator()
            .Validate(new CreateMicrosoftTeamsWebhookRequest(url)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("http://tenant.webhook.office.com/webhookb2/id/IncomingWebhook/fake/channel")]
    [InlineData("https://tenant.webhook.office.com:8443/webhookb2/id/IncomingWebhook/fake/channel")]
    [InlineData("https://user:pass@tenant.webhook.office.com/webhookb2/id/IncomingWebhook/fake/channel")]
    [InlineData("https://tenant.webhook.office.com/webhookb2/id/IncomingWebhook/fake/channel#fragment")]
    [InlineData("https://tenant.webhook.office.com.evil.example/webhookb2/id/IncomingWebhook/fake/channel")]
    [InlineData("https://evilwebhook.office.com/webhookb2/id/IncomingWebhook/fake/channel")]
    [InlineData("https://127.0.0.1/webhookb2/id/IncomingWebhook/fake/channel")]
    [InlineData("https://outlook.office.com/mail")]
    [InlineData("https://test.logic.azure.com/workflows/id/triggers/manual/paths/invoke")]
    [InlineData("https://test.logic.azure.com/workflows/id/triggers/manual/paths/invoke?sig=")]
    [InlineData("https://test.logic.azure.com/other?sig=fake")]
    [InlineData("https://test.environment.api.powerplatform.com/powerautomate/automations/direct/workflows/id/triggers/manual/paths/invoke?sig=fake#fragment")]
    public void TryParse_ShouldRejectUnsupportedWebhookUrls(string? url)
    {
        MicrosoftTeamsWebhookAddress.TryParse(url, out var uri).Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void Validator_ShouldRejectOverlongUrl()
    {
        var url = "https://test.logic.azure.com/workflows/id/triggers/manual/paths/invoke?sig=" +
                  new string('x', 4096);
        new CreateMicrosoftTeamsWebhookRequestValidator()
            .Validate(new CreateMicrosoftTeamsWebhookRequest(url)).IsValid.Should().BeFalse();
    }
}
