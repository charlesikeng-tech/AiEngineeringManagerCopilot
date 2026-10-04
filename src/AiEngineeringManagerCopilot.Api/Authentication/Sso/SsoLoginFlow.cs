using System.Net.Mail;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using AiEngineeringManagerCopilot.Api.Endpoints;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.Api.Authentication.Sso;

public sealed record SsoActiveSnapshot(string Type, string Name, string Authority, string ClientId, string? ProtectedSecret)
{
    public SsoProvider ProtocolProvider() => new()
    {
        Type = Type, Name = Name, Authority = Authority, ClientId = ClientId, ProtectedSecret = ProtectedSecret
    };
}

public sealed class SsoLoginFlow(
    IOptionsFactory<OpenIdConnectOptions> optionsFactory, SsoOidcHandlerFactory handlers,
    SsoProtocolHttpClientFactory clients, SsoDeployment deployment, AppDbContext db, IWebHostEnvironment environment,
    SsoLoginDiagnostics diagnostics, ILogger<SsoLoginFlow> logger)
{
    public const string Scheme = "SsoLogin";
    public const string CallbackPath = "/auth/sso/login/callback";

    public async Task<string> StartAsync(HttpContext context, SsoLoginAttempt attempt)
    {
        var urls = deployment.GetLoginUrls();
        diagnostics.Stage = SsoLoginStage.ActiveSnapshot;
        var snapshot = JsonSerializer.Deserialize<SsoActiveSnapshot>(attempt.ActiveConfiguration)!;
        diagnostics.Stage = SsoLoginStage.Backchannel;
        using var backchannel = clients.Create(snapshot.Authority);
        var options = handlers.Create(snapshot.ProtocolProvider(), attempt.Id, Scheme, urls.Callback, CallbackPath, backchannel);
        if (attempt.LinkTargetUserId is not null)
        {
            var redirect = options.Events.OnRedirectToIdentityProvider;
            options.Events.OnRedirectToIdentityProvider = async eventContext =>
            {
                await redirect(eventContext);
                eventContext.ProtocolMessage.Prompt = "login";
                eventContext.ProtocolMessage.MaxAge = "0";
            };
        }
        var properties = new AuthenticationProperties();
        properties.Items[SsoOidcHandlerFactory.AttemptKey] = attempt.Id.ToString("D");
        diagnostics.Stage = SsoLoginStage.HandlerInitialization;
        var handler = await SsoOidcHandlerFactory.HandlerAsync(context, options, Scheme);
        diagnostics.Stage = SsoLoginStage.AuthorizationChallenge;
        await handler.ChallengeAsync(properties);
        diagnostics.Stage = SsoLoginStage.AuthorizationRedirect;
        var target = context.Response.Headers.Location.ToString();
        context.Response.Headers.Remove("Location");
        context.Response.StatusCode = StatusCodes.Status200OK;
        return target.Length > 0 ? target : throw new InvalidOperationException("No authorization request.");
    }

    public async Task CallbackAsync(HttpContext context, CancellationToken ct)
    {
        var urls = deployment.GetLoginUrls();
        var state = context.Request.Query["state"].ToString();
        var properties = state.Length <= 16384 ? optionsFactory.Create(Scheme).StateDataFormat.Unprotect(state) : null;
        if (properties is null || !properties.Items.TryGetValue(SsoOidcHandlerFactory.AttemptKey, out var value) ||
            !Guid.TryParseExact(value, "D", out var id))
        {
            Redirect(context, urls.Frontend, "login_failed");
            return;
        }
        var attempt = await db.SsoLoginAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (attempt is null) { Redirect(context, urls.Frontend, "login_failed"); return; }
        void Fail(string error = "link_failed")
        {
            if (attempt.LinkTargetUserId is not null)
            {
                logger.LogInformation("Account linking: user {UserId}, attempt {AttemptId}, outcome {Outcome}",
                    attempt.LinkTargetUserId, attempt.Id, error);
                context.Response.Redirect(urls.Frontend + "/account/security?link=" + error);
            }
            else Redirect(context, urls.Frontend, error == "link_failed" ? "login_failed" : error);
        }
        var consumed = await db.SsoLoginAttempts.Where(x => x.Id == id && !x.Consumed && x.ExpiresAt > DateTimeOffset.UtcNow &&
            (x.LinkTargetUserId == null || (db.AdministratorSessions.Any(s =>
                s.TokenHash == x.LinkSourceSessionHash && s.UserId == x.LinkTargetUserId) &&
                AdministratorSessionProof.ValidHashes(db).Contains(x.LinkSourceSessionHash!))) &&
            db.SsoProviders.Any(p => p.Id == x.ProviderId && p.ActiveRevision == x.ActiveRevision &&
                p.ActiveConfiguration == x.ActiveConfiguration)).ExecuteUpdateAsync(s => s.SetProperty(x => x.Consumed, true), ct);
        if (consumed != 1) { Fail(); return; }
        var snapshot = JsonSerializer.Deserialize<SsoActiveSnapshot>(attempt.ActiveConfiguration)!;
        using var backchannel = clients.Create(snapshot.Authority);
        try
        {
            var options = handlers.Create(snapshot.ProtocolProvider(), id, Scheme, urls.Callback, CallbackPath, backchannel);
            options.Events.OnRemoteFailure = eventContext =>
            {
                eventContext.HandleResponse();
                Fail();
                return Task.CompletedTask;
            };
            options.Events.OnTicketReceived = async eventContext =>
            {
                eventContext.HandleResponse();
                var subject = eventContext.Principal?.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(subject) || subject.Length > 255)
                {
                    Fail();
                    return;
                }
                if (attempt.LinkTargetUserId is not null &&
                    (!long.TryParse(eventContext.Principal?.FindFirst("auth_time")?.Value,
                        NumberStyles.None, CultureInfo.InvariantCulture, out var authenticatedAt) ||
                     authenticatedAt < attempt.ExpiresAt.AddMinutes(-10).AddSeconds(-30).ToUnixTimeSeconds() ||
                     authenticatedAt > DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds()))
                {
                    Fail();
                    return;
                }
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                // Shared with configuration mutations: recheck the exact active snapshot and
                // serialize JIT uniqueness across concurrent callbacks/processes.
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7192836402)", ct);
                if (attempt.ExpiresAt <= DateTimeOffset.UtcNow ||
                    !await db.SsoLoginAttempts.AnyAsync(x => x.Id == attempt.Id && x.Consumed, ct) ||
                    !await db.SsoProviders.AnyAsync(p => p.Id == attempt.ProviderId &&
                        p.ActiveRevision == attempt.ActiveRevision && p.ActiveConfiguration == attempt.ActiveConfiguration, ct))
                {
                    Fail();
                    return;
                }
                var identity = await db.ExternalIdentities.SingleOrDefaultAsync(
                    x => x.Issuer == snapshot.Authority && x.Subject == subject, ct);
                if (attempt.LinkTargetUserId is Guid targetId)
                {
                    if (attempt.LinkSourceSessionHash is null ||
                        await AdministratorSessionProof.LockLocalAsync(db, attempt.LinkSourceSessionHash, targetId, ct) is null)
                    {
                        Fail();
                        return;
                    }
                    if (identity is not null && identity.UserId != targetId)
                    {
                        Fail("identity_conflict");
                        return;
                    }
                    if (identity is null)
                    {
                        identity = new ExternalIdentity
                        {
                            Id = Guid.NewGuid(), UserId = targetId, Issuer = snapshot.Authority, Subject = subject
                        };
                        db.ExternalIdentities.Add(identity);
                    }
                    identity.AdministratorAccessApproved = true;
                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    logger.LogInformation("Account linking: user {UserId}, identity {IdentityId}, outcome {Outcome}",
                        targetId, identity.Id, "linked");
                    context.Response.Redirect(urls.Frontend + "/account/security?link=success");
                    return;
                }
                User user;
                if (identity is null)
                {
                    var emailClaim = eventContext.Principal?.FindFirst("email")?.Value;
                    var email = ValidEmail(emailClaim) ? emailClaim!.Trim().ToLowerInvariant() : null;
                    if (email is not null && await db.Users.AnyAsync(x => x.Email != null && x.Email.ToLower() == email, ct))
                    {
                        Redirect(context, urls.Frontend, "email_collision");
                        return;
                    }
                    var name = eventContext.Principal?.FindFirst("name")?.Value?.Trim();
                    if (string.IsNullOrEmpty(name) || name.Length > 200 || name.Any(char.IsControl)) name = "SSO user";
                    user = new User
                    {
                        Id = Guid.NewGuid(), Email = email, Name = name, CreatedAt = DateTimeOffset.UtcNow,
                        EmailVerified = email is not null &&
                            eventContext.Principal?.FindFirst("email_verified")?.Value == "true"
                    };
                    identity = new ExternalIdentity { Id = Guid.NewGuid(), UserId = user.Id, Issuer = snapshot.Authority, Subject = subject };
                    db.Users.Add(user);
                    db.ExternalIdentities.Add(identity);
                }
                else
                {
                    user = await db.Users.SingleAsync(x => x.Id == identity.UserId, ct);
                    var admin = await db.LocalAdministrators.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == user.Id, ct);
                    if (!user.IsActive || (admin is not null && (!identity.AdministratorAccessApproved ||
                        !admin.IsActive || admin.Role != LocalSessionAuthenticationHandler.AdministratorRole)) ||
                        (identity.AdministratorAccessApproved && admin is null))
                    {
                        Redirect(context, urls.Frontend, "login_failed");
                        return;
                    }
                }
                await db.SsoSessions.Where(x => x.ExpiresAt <= DateTimeOffset.UtcNow).ExecuteDeleteAsync(ct);
                await LocalAuthenticationEndpoints.RevokePresentedSessionAsync(context, db, ct);
                if (attempt.PreviousSessionHash is not null)
                {
                    await db.AdministratorSessions.Where(x => x.TokenHash == attempt.PreviousSessionHash).ExecuteDeleteAsync(ct);
                    await db.SsoSessions.Where(x => x.TokenHash == attempt.PreviousSessionHash).ExecuteDeleteAsync(ct);
                }
                var token = SsoSessionAuthenticationHandler.TokenPrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                db.SsoSessions.Add(new SsoSession
                {
                    TokenHash = LocalSessionAuthenticationHandler.Hash(token), UserId = user.Id, ExternalIdentityId = identity.Id,
                    ProviderId = attempt.ProviderId, ActiveRevision = attempt.ActiveRevision,
                    ActiveConfiguration = attempt.ActiveConfiguration, ExpiresAt = DateTimeOffset.UtcNow.AddHours(8)
                });
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                LocalAuthenticationEndpoints.WriteCookie(context, environment, token);
                context.Response.Redirect(urls.Frontend + "/dashboard");
            };
            var handler = await SsoOidcHandlerFactory.HandlerAsync(context, options, Scheme);
            if (!await handler.HandleRequestAsync()) Fail();
        }
        catch (Exception ex) when (SsoConnectionFlow.IsProtocolFailure(ex, ct))
        {
            Fail();
        }
    }

    private static bool ValidEmail(string? email) => !string.IsNullOrWhiteSpace(email) && email.Trim().Length <= 254 &&
        MailAddress.TryCreate(email.Trim(), out var parsed) && parsed.Address == email.Trim();

    private static void Redirect(HttpContext context, string frontend, string error) =>
        context.Response.Redirect(frontend + "/login?ssoError=" + error);
}
