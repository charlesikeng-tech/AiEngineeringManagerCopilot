using System.Security.Claims;
using System.Text.Json;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Api.Authentication.Sso;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class AccountLinkEndpoints
{
    public sealed record LinkRequest(Guid ProviderId, string Password, bool ApproveAdministratorAccess)
    {
        public override string ToString() => "Account link request; password redacted";
    }
    public sealed record UnlinkRequest(string Password)
    {
        public override string ToString() => "Account unlink request; password redacted";
    }

    public static void MapAccountLinkEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth/account/identities");
        group.MapGet("", async (HttpContext context, AppDbContext db, CancellationToken ct) =>
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var identities = await db.ExternalIdentities.AsNoTracking().Where(x => x.UserId == userId)
                .Select(x => new { x.Id, x.Issuer, x.AdministratorAccessApproved }).ToListAsync(ct);
            var snapshots = (await db.SsoProviders.AsNoTracking().Where(x => x.ActiveConfiguration != null)
                .Select(x => x.ActiveConfiguration!).ToListAsync(ct))
                .Select(x => JsonSerializer.Deserialize<SsoActiveSnapshot>(x)!).ToList();
            return Results.Ok(new
            {
                canLink = context.User.Identity?.AuthenticationType == LocalSessionAuthenticationHandler.Scheme,
                identities = identities.Select(x => new
                {
                    x.Id, x.Issuer, x.AdministratorAccessApproved,
                    providerName = snapshots.FirstOrDefault(p => p.Authority == x.Issuer)?.Name
                })
            });
        }).RequireAuthorization("PlatformAdministrator");
        group.MapPost("/start", StartAsync).RequireAuthorization("LocalAdministrator")
            .RequireRateLimiting("LocalAuthentication");
        group.MapPost("/{id:guid}/unlink", UnlinkAsync).RequireAuthorization("LocalAdministrator")
            .RequireRateLimiting("LocalAuthentication");
    }

    private static async Task<IResult> StartAsync(LinkRequest request, HttpContext context, AppDbContext db,
        IPasswordHasher<IdentityUser> hasher, SsoLoginFlow flow, SsoDeployment deployment,
        SsoLoginDiagnostics diagnostics, CancellationToken ct)
    {
        if (!request.ApproveAdministratorAccess) return Results.BadRequest(new { error = "consent_required" });
        try { deployment.GetLoginUrls(); }
        catch (InvalidOperationException) { return Results.BadRequest(new { error = "deployment_configuration" }); }
        var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var hash = LocalSessionAuthenticationHandler.Hash(context.Request.Cookies[LocalSessionAuthenticationHandler.CookieName]!);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7192836402)", ct);
        if (await AdministratorSessionProof.LockLocalAsync(db, hash, userId, ct) is null) return Results.Unauthorized();
        var admin = await db.LocalAdministrators.SingleAsync(x => x.UserId == userId, ct);
        if (await LocalPasswordConfirmation.VerifyAsync(admin, request.Password, db, hasher, ct) is null)
        {
            await transaction.CommitAsync(ct);
            return Results.Unauthorized();
        }
        var provider = await db.SsoProviders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.ProviderId &&
            x.ActiveRevision != null && x.ActiveConfiguration != null, ct);
        if (provider is null) return Results.NotFound();
        await db.SsoLoginAttempts.Where(x => x.ExpiresAt <= DateTimeOffset.UtcNow).ExecuteDeleteAsync(ct);
        var attempt = new SsoLoginAttempt
        {
            Id = Guid.NewGuid(), ProviderId = provider.Id, ActiveRevision = provider.ActiveRevision!.Value,
            ActiveConfiguration = provider.ActiveConfiguration!, LinkTargetUserId = userId,
            LinkSourceSessionHash = hash, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        db.SsoLoginAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        try { return Results.Ok(new { authorizationUrl = await flow.StartAsync(context, attempt) }); }
        catch (Exception ex) when (SsoConnectionFlow.IsProtocolFailure(ex, ct))
        {
            var diagnosticId = diagnostics.ReportFailure(ex, provider.Id, attempt.ActiveRevision);
            await db.SsoLoginAttempts.Where(x => x.Id == attempt.Id).ExecuteDeleteAsync(ct);
            return Results.BadRequest(new { error = "link_failed", diagnosticId });
        }
    }

    private static async Task<IResult> UnlinkAsync(Guid id, UnlinkRequest request, HttpContext context, AppDbContext db,
        IPasswordHasher<IdentityUser> hasher, ILoggerFactory loggers, CancellationToken ct)
    {
        var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var hash = LocalSessionAuthenticationHandler.Hash(context.Request.Cookies[LocalSessionAuthenticationHandler.CookieName]!);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7192836402)", ct);
        if (await AdministratorSessionProof.LockLocalAsync(db, hash, userId, ct) is null) return Results.Unauthorized();
        var admin = await db.LocalAdministrators.SingleAsync(x => x.UserId == userId, ct);
        if (await LocalPasswordConfirmation.VerifyAsync(admin, request.Password, db, hasher, ct) is null)
        {
            await transaction.CommitAsync(ct);
            return Results.Unauthorized();
        }
        var identity = await db.ExternalIdentities.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (identity is null) return Results.NotFound();
        await db.SsoSessions.Where(x => x.ExternalIdentityId == id).ExecuteDeleteAsync(ct);
        // Invalidate in-flight links so an old callback cannot silently undo revocation.
        await db.SsoLoginAttempts.Where(x => x.LinkTargetUserId == userId).ExecuteDeleteAsync(ct);
        db.ExternalIdentities.Remove(identity);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        loggers.CreateLogger("AccountLinkAudit").LogInformation(
            "Account identity change: user {UserId}, identity {IdentityId}, outcome {Outcome}", userId, id, "unlinked");
        return Results.NoContent();
    }
}
