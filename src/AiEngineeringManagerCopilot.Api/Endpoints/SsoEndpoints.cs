using System.Text.Json;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Api.Authentication.Sso;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class SsoEndpoints
{
    public sealed record DraftRequest(int? Revision, string Type, string Name, string Tenant,
        string? AuthorizationServer, string ClientId, string SecretAction, string? ClientSecret)
    {
        public override string ToString() => $"SSO draft ({Type}); credentials redacted";
    }
    public sealed record RevisionRequest(int Revision);
    private sealed record Snapshot(string Type, string Name, string Authority, string ClientId, string? ProtectedSecret);

    public static void MapSsoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth/sso/providers").RequireAuthorization("LocalAdministrator");
        group.MapGet("", async (AppDbContext db, SsoDeployment deployment, CancellationToken ct) =>
        {
            (string Callback, string Frontend) urls;
            try { urls = deployment.GetUrls(); }
            catch (InvalidOperationException) { return Results.BadRequest(new { error = "deployment_configuration" }); }
            var providers = await db.SsoProviders.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
            return Results.Ok(new { callbackUrl = urls.Callback, providers = providers.Select(View) });
        });
        group.MapPost("", (DraftRequest request, AppDbContext db, ISecretProtector secrets, CancellationToken ct) =>
            SaveAsync(null, request, db, secrets, ct));
        group.MapPut("/{id:guid}", (Guid id, DraftRequest request, AppDbContext db, ISecretProtector secrets, CancellationToken ct) =>
            SaveAsync(id, request, db, secrets, ct));
        group.MapPost("/{id:guid}/test", StartTestAsync);
        group.MapPost("/{id:guid}/activate", async (Guid id, RevisionRequest request, AppDbContext db, CancellationToken ct) =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await LockAsync(db, ct);
            var provider = await db.SsoProviders.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (provider is null) return Results.NotFound();
            if (provider.Revision != request.Revision || provider.TestedRevision != request.Revision)
                return Results.Conflict(new { error = "test_required" });
            provider.ActiveConfiguration = JsonSerializer.Serialize(new Snapshot(
                provider.Type, provider.Name, provider.Authority, provider.ClientId, provider.ProtectedSecret));
            provider.ActiveRevision = provider.Revision;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(View(provider));
        });
        group.MapPost("/{id:guid}/deactivate", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await LockAsync(db, ct);
            var provider = await db.SsoProviders.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (provider is null) return Results.NotFound();
            provider.ActiveConfiguration = null;
            provider.ActiveRevision = null;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(View(provider));
        });
        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await LockAsync(db, ct);
            var provider = await db.SsoProviders.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (provider is null) return Results.NotFound();
            if (provider.ActiveRevision is not null) return Results.Conflict(new { error = "deactivate_required" });
            db.SsoProviders.Remove(provider);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        });
        app.MapGet(SsoDeployment.CallbackPath, async (HttpContext context, SsoConnectionFlow flow, CancellationToken ct) =>
        {
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await flow.CallbackAsync(context, ct);
        }).AllowAnonymous();
    }

    private static async Task<IResult> SaveAsync(Guid? id, DraftRequest request, AppDbContext db,
        ISecretProtector secrets, CancellationToken ct)
    {
        string authority;
        try { authority = SsoAuthority.Normalize(request.Type, request.Tenant ?? "", request.AuthorizationServer); }
        catch (ArgumentException) { return Results.BadRequest(new { error = "invalid_provider" }); }
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(request.ClientId) || request.ClientId.Trim().Length > 256 ||
            request.SecretAction is not ("retain" or "replace" or "clear") ||
            (request.SecretAction == "replace" && (string.IsNullOrWhiteSpace(request.ClientSecret) || request.ClientSecret.Length > 4096)) ||
            (request.SecretAction != "replace" && !string.IsNullOrEmpty(request.ClientSecret)))
            return Results.BadRequest(new { error = "invalid_provider" });
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(db, ct);
        var provider = id is null ? new SsoProvider { Id = Guid.NewGuid() } :
            await db.SsoProviders.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (provider is null) return Results.NotFound();
        if (id is not null && provider.Revision != request.Revision)
            return Results.Conflict(new { error = "revision_conflict" });
        if (id is null) db.SsoProviders.Add(provider);
        else provider.Revision++;
        provider.Type = request.Type;
        provider.Name = request.Name.Trim();
        provider.Authority = authority;
        provider.ClientId = request.ClientId.Trim();
        if (request.SecretAction == "replace") provider.ProtectedSecret = secrets.Protect(request.ClientSecret!);
        if (request.SecretAction == "clear") provider.ProtectedSecret = null;
        provider.TestedRevision = null;
        provider.TestedAt = null;
        provider.LastTestError = null;
        provider.CurrentTestId = null;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(View(provider));
    }

    private static async Task<IResult> StartTestAsync(Guid id, RevisionRequest request, HttpContext context,
        AppDbContext db, SsoConnectionFlow flow, SsoDeployment deployment, CancellationToken ct)
    {
        try { deployment.GetUrls(); }
        catch (InvalidOperationException) { return Results.BadRequest(new { error = "deployment_configuration" }); }
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(db, ct);
        var provider = await db.SsoProviders.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (provider is null) return Results.NotFound();
        if (provider.Revision != request.Revision) return Results.Conflict(new { error = "revision_conflict" });
        var hash = LocalSessionAuthenticationHandler.Hash(context.Request.Cookies[LocalSessionAuthenticationHandler.CookieName]!);
        await db.SsoConnectionTests.Where(x => x.ExpiresAt <= DateTimeOffset.UtcNow).ExecuteDeleteAsync(ct);
        // Only the newest test can complete for this revision; multiple tabs cannot race their results.
        await db.SsoConnectionTests.Where(x => x.ProviderId == id).ExecuteUpdateAsync(
            setters => setters.SetProperty(x => x.Consumed, true), ct);
        var attempt = new SsoConnectionTest
        {
            Id = Guid.NewGuid(), ProviderId = id, Revision = provider.Revision, SessionHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        db.SsoConnectionTests.Add(attempt);
        provider.CurrentTestId = attempt.Id;
        provider.TestedRevision = null;
        provider.TestedAt = null;
        provider.LastTestError = null;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        try { return Results.Ok(new { authorizationUrl = await flow.StartAsync(context, provider, attempt) }); }
        catch (Exception ex) when (SsoConnectionFlow.IsProtocolFailure(ex, ct))
        {
            await flow.MarkFailedAsync(attempt, ct);
            return Results.BadRequest(new { error = "connection_failed" });
        }
    }

    private static object View(SsoProvider provider)
    {
        var active = provider.ActiveConfiguration is null ? null : JsonSerializer.Deserialize<Snapshot>(provider.ActiveConfiguration);
        return new
        {
            provider.Id, provider.Type, provider.Name, provider.Authority, provider.ClientId,
            hasSecret = provider.ProtectedSecret is not null, provider.Revision,
            provider.TestedRevision, provider.TestedAt, provider.LastTestError, provider.ActiveRevision,
            active = active is null ? null : new { active.Type, active.Name, active.Authority, active.ClientId, hasSecret = active.ProtectedSecret is not null }
        };
    }

    private static Task LockAsync(AppDbContext db, CancellationToken ct) =>
        db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7192836402)", ct);
}
