using AiEngineeringManagerCopilot.Application.Jira;
using AiEngineeringManagerCopilot.Application.Common;

namespace AiEngineeringManagerCopilot.UnitTests.Jira;

public sealed class JiraDestinationTests
{
    [Theory]
    [InlineData("http://tenant.atlassian.net")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://localhost")]
    [InlineData("https://tenant.atlassian.net.attacker.com")]
    [InlineData("https://tenant.atlassian.net:8443")]
    [InlineData("https://user:pass@tenant.atlassian.net")]
    [InlineData("https://tenant.atlassian.net/path")]
    [InlineData("https://tenant.atlassian.net?target=internal")]
    public void RejectsUnsafeDestinations(string url)
    {
        Assert.Throws<ValidationException>(() => JiraDestination.Validate(url));
    }

    [Fact]
    public void AllowsJiraCloudTenant()
    {
        Assert.Equal("tenant.atlassian.net", JiraDestination.Validate("https://tenant.atlassian.net/").Host);
    }
}
