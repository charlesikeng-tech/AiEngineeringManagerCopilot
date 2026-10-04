using System.Security.Claims;
using System.Text.Encodings.Web;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.Api.Authentication;

public sealed class SsoSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    AppDbContext db, IWebHostEnvironment environment) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public new const string Scheme = "SsoSession";
    public const string UserRole = "User";
    public const string TokenPrefix = "s.";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps && !environment.IsDevelopment() && !environment.IsEnvironment("Test"))
            return AuthenticateResult.NoResult();
        if (!Request.Cookies.TryGetValue(LocalSessionAuthenticationHandler.CookieName, out var token) ||
            token.Length != 66 || !token.StartsWith(TokenPrefix, StringComparison.Ordinal))
            return AuthenticateResult.NoResult();
        var hash = LocalSessionAuthenticationHandler.Hash(token);
        var account = await (
            from session in db.SsoSessions.AsNoTracking()
            join identity in db.ExternalIdentities.AsNoTracking() on session.ExternalIdentityId equals identity.Id
            join user in db.Users.AsNoTracking() on session.UserId equals user.Id
            join provider in db.SsoProviders.AsNoTracking() on session.ProviderId equals provider.Id
            where session.TokenHash == hash && session.ExpiresAt > DateTimeOffset.UtcNow &&
                user.IsActive && identity.UserId == user.Id &&
                provider.ActiveRevision == session.ActiveRevision &&
                provider.ActiveConfiguration == session.ActiveConfiguration &&
                ((!identity.AdministratorAccessApproved && !db.LocalAdministrators.Any(a => a.UserId == user.Id)) ||
                    (identity.AdministratorAccessApproved && db.LocalAdministrators.Any(a => a.UserId == user.Id &&
                        a.IsActive && a.Role == LocalSessionAuthenticationHandler.AdministratorRole)))
            select new { user.Id, user.Email, user.Name, identity.AdministratorAccessApproved }).SingleOrDefaultAsync(Context.RequestAborted);
        if (account is null) return AuthenticateResult.Fail("Invalid or expired session.");
        var identityClaims = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Name),
            new Claim(ClaimTypes.Role, account.AdministratorAccessApproved ? LocalSessionAuthenticationHandler.AdministratorRole : UserRole)
        ], Scheme);
        if (account.Email is not null) identityClaims.AddClaim(new Claim(ClaimTypes.Email, account.Email));
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identityClaims), Scheme));
    }
}
