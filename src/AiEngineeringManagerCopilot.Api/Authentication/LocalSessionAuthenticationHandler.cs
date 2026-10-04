using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.Api.Authentication;

public sealed class LocalSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AppDbContext db,
    IWebHostEnvironment environment) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public new const string Scheme = "LocalSession";
    public const string CookieName = "aem.admin.session";
    public const string AdministratorRole = "PlatformAdministrator";
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps && !environment.IsDevelopment() && !environment.IsEnvironment("Test"))
            return AuthenticateResult.NoResult();
        if (!Request.Cookies.TryGetValue(CookieName, out var token) || token.Length != 64)
            return AuthenticateResult.NoResult();

        var hash = Hash(token);
        var account = await (
            from session in db.AdministratorSessions.AsNoTracking()
            join admin in db.LocalAdministrators.AsNoTracking() on session.UserId equals admin.UserId
            join user in db.Users.AsNoTracking() on admin.UserId equals user.Id
            where session.TokenHash == hash && session.ExpiresAt > DateTimeOffset.UtcNow &&
                  admin.IsActive && user.IsActive && admin.Role == AdministratorRole
            select new { user.Id, user.Email, user.Name }).SingleOrDefaultAsync(Context.RequestAborted);

        if (account is null) return AuthenticateResult.Fail("Invalid or expired session.");
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Email, account.Email ?? ""),
            new Claim(ClaimTypes.Name, account.Name),
            new Claim(ClaimTypes.Role, AdministratorRole)
        ], Scheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme));
    }
}
