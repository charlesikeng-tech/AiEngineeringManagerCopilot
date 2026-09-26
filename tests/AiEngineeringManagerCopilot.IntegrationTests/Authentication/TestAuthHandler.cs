using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.IntegrationTests.Authentication;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options,
        logger,
        encoder)
{
    public const string SchemeName = "Test";

    public static readonly Guid UserId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(
                "X-Test-Unauthenticated"))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
            
        }
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                UserId.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            SchemeName);

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}