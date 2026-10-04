using System.Text.Json;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Api.Authentication.Sso;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class SsoLoginEndpoints
{
    public static void MapSsoLoginEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth/sso/login").AllowAnonymous();
        group.MapGet("/providers", async (AppDbContext db, CancellationToken ct) =>
        {
            var providers = await db.SsoProviders.AsNoTracking()
                .Where(x => x.ActiveRevision != null && x.ActiveConfiguration != null).ToListAsync(ct);
            return Results.Ok(providers.Select(p =>
            {
                var snapshot = JsonSerializer.Deserialize<SsoActiveSnapshot>(p.ActiveConfiguration!)!;
                return new { p.Id, snapshot.Name, snapshot.Type };
            }).OrderBy(x => x.Name));
        });
        group.MapPost("/{id:guid}/start", async (Guid id, HttpContext context, AppDbContext db,
            SsoLoginFlow flow, SsoDeployment deployment, CancellationToken ct) =>
        {
            try { deployment.GetLoginUrls(); }
            catch (InvalidOperationException) { return Results.BadRequest(new { error = "deployment_configuration" }); }
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7192836402)", ct);
            var provider = await db.SsoProviders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id &&
                x.ActiveRevision != null && x.ActiveConfiguration != null, ct);
            if (provider is null) return Results.NotFound();
            await db.SsoLoginAttempts.Where(x => x.ExpiresAt <= DateTimeOffset.UtcNow).ExecuteDeleteAsync(ct);
            var token = context.Request.Cookies[LocalSessionAuthenticationHandler.CookieName];
            var attempt = new SsoLoginAttempt
            {
                Id = Guid.NewGuid(), ProviderId = id, ActiveRevision = provider.ActiveRevision!.Value,
                ActiveConfiguration = provider.ActiveConfiguration!, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                PreviousSessionHash = token is null ? null : LocalSessionAuthenticationHandler.Hash(token)
            };
            db.SsoLoginAttempts.Add(attempt);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            try { return Results.Ok(new { authorizationUrl = await flow.StartAsync(context, attempt) }); }
            catch (Exception ex) when (SsoConnectionFlow.IsProtocolFailure(ex, ct))
            {
                await db.SsoLoginAttempts.Where(x => x.Id == attempt.Id).ExecuteDeleteAsync(ct);
                return Results.BadRequest(new { error = "login_failed" });
            }
        }).RequireRateLimiting("LocalAuthentication");
        group.MapGet("/callback", async (HttpContext context, SsoLoginFlow flow, CancellationToken ct) =>
        {
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await flow.CallbackAsync(context, ct);
        });
    }
}
