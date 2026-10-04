using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;

namespace AiEngineeringManagerCopilot.Api.Authentication.Sso;

// The framework handler owns state encryption, correlation, nonce, PKCE, code exchange
// and token validation. Its validated ticket is deliberately never signed into any scheme.
public sealed class SsoConnectionFlow(
    IOptionsFactory<OpenIdConnectOptions> optionsFactory,
    SsoOidcHandlerFactory handlers,
    SsoProtocolHttpClientFactory clients,
    SsoDeployment deployment,
    AppDbContext db)
{
    public const string Scheme = "SsoConnectionTest";
    private const string AttemptKey = SsoOidcHandlerFactory.AttemptKey;

    public async Task<string> StartAsync(HttpContext context, SsoProvider provider, SsoConnectionTest attempt)
    {
        var urls = deployment.GetUrls();
        using var backchannel = clients.Create(provider.Authority);
        var options = BuildOptions(provider, attempt, urls, backchannel);
        var handler = await SsoOidcHandlerFactory.HandlerAsync(context, options, Scheme);
        var properties = new AuthenticationProperties();
        properties.Items[AttemptKey] = attempt.Id.ToString("D");
        await handler.ChallengeAsync(properties);
        var authorizationUrl = context.Response.Headers.Location.ToString();
        context.Response.Headers.Remove("Location");
        context.Response.StatusCode = StatusCodes.Status200OK;
        return authorizationUrl.Length > 0 ? authorizationUrl :
            throw new InvalidOperationException("Provider did not produce an authorization request.");
    }

    public async Task CallbackAsync(HttpContext context, CancellationToken ct)
    {
        var urls = deployment.GetUrls();
        // Decode only authenticated framework state; never trust a provider id from query parameters.
        var state = context.Request.Query["state"].ToString();
        var properties = state.Length <= 16384 ? optionsFactory.Create(Scheme).StateDataFormat.Unprotect(state) : null;
        if (properties is null || !properties.Items.TryGetValue(AttemptKey, out var value) ||
            !Guid.TryParseExact(value, "D", out var attemptId))
        {
            Redirect(context, urls.Frontend, false);
            return;
        }
        var attempt = await db.SsoConnectionTests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attemptId, ct);
        if (attempt is null) { Redirect(context, urls.Frontend, false); return; }

        // Consume atomically before exchanging a code. Replays cannot issue a second token request.
        var consumed = await db.SsoConnectionTests.Where(x => x.Id == attempt.Id && !x.Consumed &&
                x.ExpiresAt > DateTimeOffset.UtcNow && db.SsoProviders.Any(p => p.Id == x.ProviderId && p.Revision == x.Revision) &&
                AdministratorSessionProof.ValidHashes(db).Contains(x.SessionHash))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Consumed, true), ct);
        if (consumed != 1) { Redirect(context, urls.Frontend, false); return; }
        var provider = await db.SsoProviders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attempt.ProviderId, ct);
        if (provider is null || provider.Revision != attempt.Revision || provider.CurrentTestId != attempt.Id)
        {
            Redirect(context, urls.Frontend, false);
            return;
        }
        using var backchannel = clients.Create(provider.Authority);
        try
        {
            var options = BuildOptions(provider, attempt, urls, backchannel);
            options.Events.OnTicketReceived = async eventContext =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7192836402)", ct);
                var updated = await db.SsoProviders.Where(p => p.Id == attempt.ProviderId && p.Revision == attempt.Revision &&
                        p.CurrentTestId == attempt.Id && attempt.ExpiresAt > DateTimeOffset.UtcNow &&
                        AdministratorSessionProof.ValidHashes(db).Contains(attempt.SessionHash))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.TestedRevision, attempt.Revision)
                        .SetProperty(p => p.TestedAt, DateTimeOffset.UtcNow)
                        .SetProperty(p => p.LastTestError, (string?)null), ct);
                await transaction.CommitAsync(ct);
                eventContext.HandleResponse();
                Redirect(context, urls.Frontend, updated == 1);
            };
            options.Events.OnRemoteFailure = async eventContext =>
            {
                await MarkFailedAsync(attempt, ct);
                eventContext.HandleResponse();
                Redirect(context, urls.Frontend, false);
            };
            var handler = await SsoOidcHandlerFactory.HandlerAsync(context, options, Scheme);
            if (!await handler.HandleRequestAsync())
            {
                await MarkFailedAsync(attempt, ct);
                Redirect(context, urls.Frontend, false);
            }
        }
        catch (Exception ex) when (IsProtocolFailure(ex, ct))
        {
            // Protocol exceptions can contain provider responses/claims: persist only a fixed code.
            await MarkFailedAsync(attempt, ct);
            Redirect(context, urls.Frontend, false);
        }

    }

    public static bool IsProtocolFailure(Exception exception, CancellationToken ct) =>
        exception is AuthenticationFailureException or HttpRequestException or SecurityTokenException or
            OpenIdConnectProtocolException or CryptographicException or InvalidOperationException or JsonException ||
        (exception is OperationCanceledException && !ct.IsCancellationRequested);

    public async Task MarkFailedAsync(SsoConnectionTest attempt, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7192836402)", ct);
        await db.SsoProviders.Where(p => p.Id == attempt.ProviderId && p.Revision == attempt.Revision && p.CurrentTestId == attempt.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.TestedRevision, (int?)null)
                .SetProperty(p => p.TestedAt, (DateTimeOffset?)null)
                .SetProperty(p => p.LastTestError, "connection_failed"), ct);
        await transaction.CommitAsync(ct);
    }

    private OpenIdConnectOptions BuildOptions(SsoProvider provider, SsoConnectionTest attempt,
        (string Callback, string Frontend) urls, HttpClient backchannel)
        => handlers.Create(provider, attempt.Id, Scheme, urls.Callback, SsoDeployment.CallbackPath, backchannel);

    private static void Redirect(HttpContext context, string frontend, bool success) =>
        context.Response.Redirect(frontend + "?ssoTest=" + (success ? "success" : "failed"));

}
