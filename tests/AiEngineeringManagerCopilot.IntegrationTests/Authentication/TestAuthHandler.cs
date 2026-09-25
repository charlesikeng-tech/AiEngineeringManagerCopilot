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
    public const string Scheme = "Test";

    public static readonly Guid UserId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                UserId.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            Scheme);

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            Scheme);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}