using System.Net;
using AiEngineeringManagerCopilot.Api.Authentication.Sso;
using AiEngineeringManagerCopilot.Api.Endpoints;

namespace AiEngineeringManagerCopilot.IntegrationTests.Authentication;

public sealed class SsoAuthorityTests
{
    [Theory]
    [InlineData("Auth0", " Example.EU.Auth0.com ", null, "https://example.eu.auth0.com/")]
    [InlineData("Okta", "example.okta-emea.com", null, "https://example.okta-emea.com/oauth2/default")]
    [InlineData("Okta", "example.oktapreview.com", "aus123", "https://example.oktapreview.com/oauth2/aus123")]
    [InlineData("EntraId", "a5f94000-7422-4a70-98b8-8937cd00aace", null, "https://login.microsoftonline.com/a5f94000-7422-4a70-98b8-8937cd00aace/v2.0")]
    public void Normalizes_known_tenants(string type, string tenant, string? server, string expected) =>
        Assert.Equal(expected, SsoAuthority.Normalize(type, tenant, server));

    [Theory]
    [InlineData("Auth0", "https://example.auth0.com", null)]
    [InlineData("Auth0", "example.auth0.com.attacker.example", null)]
    [InlineData("Auth0", "localhost", null)]
    [InlineData("Auth0", "127.0.0.1", null)]
    [InlineData("Auth0", "xn--example.auth0.com", null)]
    [InlineData("Okta", "example.okta.com", "../admin")]
    [InlineData("Okta", "example.okta.com", "foo/bar")]
    [InlineData("EntraId", "common", null)]
    [InlineData("EntraId", "organizations", null)]
    [InlineData("EntraId", "00000000-0000-0000-0000-000000000000", null)]
    public void Rejects_unsafe_or_shared_tenants(string type, string tenant, string? server) =>
        Assert.Throws<ArgumentException>(() => SsoAuthority.Normalize(type, tenant, server));

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    [InlineData("2001:db8::1")]
    public void Rejects_nonpublic_addresses(string address) => Assert.False(SsoBackchannel.IsPublic(IPAddress.Parse(address)));

    [Fact]
    public void Request_formatting_redacts_secret() =>
        Assert.DoesNotContain("sensitive", new SsoEndpoints.DraftRequest(null, "Auth0", "Test", "example.auth0.com", null, "client", "replace", "sensitive").ToString());

    [Theory]
    [InlineData("http://example.auth0.com/token")]
    [InlineData("https://127.0.0.1/token")]
    [InlineData("https://other.auth0.com/token")]
    [InlineData("https://example.auth0.com:444/token")]
    [InlineData("https://user:password@example.auth0.com/token")]
    public async Task Backchannel_rejects_untrusted_endpoints_before_connecting(string endpoint)
    {
        using var client = new HttpClient(new SsoBackchannel("https://example.auth0.com/"));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(endpoint));
    }
}
