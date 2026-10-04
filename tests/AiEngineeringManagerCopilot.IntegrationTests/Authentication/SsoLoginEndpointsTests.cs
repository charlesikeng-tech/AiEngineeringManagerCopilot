using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Api.Authentication.Sso;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using static AiEngineeringManagerCopilot.IntegrationTests.Authentication.SsoEndpointsTests;

namespace AiEngineeringManagerCopilot.IntegrationTests.Authentication;

public sealed class SsoLoginEndpointsTests
{
    private const string Root = "/auth/sso/login";

    [Fact]
    public async Task Login_creates_ordinary_owner_profile_and_logout_revokes_session_without_seed_access()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        protocol.Email = "ordinary@test.example";
        protocol.EmailVerified = "false";
        protocol.Name = "Ordinary person";
        using var app = Configure(factory, protocol);
        var id = await Activate(app);
        using var client = NewClient(app);
        var list = await client.GetStringAsync(Root + "/providers");
        var publicProvider = JsonDocument.Parse(list).RootElement[0];
        Assert.Equal(3, publicProvider.EnumerateObject().Count());
        Assert.Equal(id, publicProvider.GetProperty("id").GetGuid());
        Assert.DoesNotContain("client", list, StringComparison.OrdinalIgnoreCase);
        var response = await Complete(client, protocol, id);
        Assert.Equal("http://localhost:4200/dashboard", response.Headers.Location!.ToString());
        SetSession(client, response);
        var profile = await Current(client);
        Assert.Equal("User", profile.GetProperty("role").GetString());
        Assert.Equal("ordinary@test.example", profile.GetProperty("email").GetString());
        Assert.False(profile.GetProperty("emailVerified").GetBoolean());
        Assert.Equal("Ordinary person", profile.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/auth/sso/providers")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/teams/{LocalAuthenticationWebApplicationFactory.SeedTeamId}")).StatusCode);
        var teams = JsonDocument.Parse(await client.GetStringAsync("/teams")).RootElement;
        Assert.Empty(teams.EnumerateArray());
        var created = await client.PostAsJsonAsync("/teams", new { name = "My team" });
        created.EnsureSuccessStatusCode();
        Assert.Single(JsonDocument.Parse(await client.GetStringAsync("/teams")).RootElement.EnumerateArray());
        client.DefaultRequestHeaders.Remove("X-Session-Protection");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/teams", new { name = "Forbidden" })).StatusCode);
        client.DefaultRequestHeaders.Add("X-Session-Protection", "1");
        var oldCookie = client.DefaultRequestHeaders.GetValues("Cookie").Single();
        var logout = await client.PostAsJsonAsync("/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), x => x.StartsWith(LocalSessionAuthenticationHandler.CookieName + "=;"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/current")).StatusCode);
        await WithDatabase(app, async db =>
        {
            Assert.Empty(await db.SsoSessions.ToListAsync());
            Assert.Single(await db.ExternalIdentities.ToListAsync());
            Assert.Single(await db.LocalAdministrators.ToListAsync());
        });
        Assert.StartsWith(LocalSessionAuthenticationHandler.CookieName + "=s.", oldCookie);
    }

    [Fact]
    public async Task Repeated_and_concurrent_first_logins_reuse_issuer_subject_and_allow_missing_email()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var first = NewClient(app);
        using var second = NewClient(app);
        var firstFlow = await Start(first, protocol, provider);
        var secondFlow = await Start(second, protocol, provider);
        var completions = await Task.WhenAll(Callback(first, firstFlow), Callback(second, secondFlow));
        SetSession(first, completions[0]);
        SetSession(second, completions[1]);
        var firstProfile = await Current(first);
        var secondProfile = await Current(second);
        Assert.Equal(firstProfile.GetProperty("id").GetGuid(), secondProfile.GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, firstProfile.GetProperty("email").ValueKind);
        Assert.False(firstProfile.GetProperty("emailVerified").GetBoolean());
        Assert.Equal("SSO user", firstProfile.GetProperty("name").GetString());
        var priorCookie = first.DefaultRequestHeaders.GetValues("Cookie").Single();
        SetSession(first, await Complete(first, protocol, provider));
        Assert.Equal(firstProfile.GetProperty("id").GetGuid(), (await Current(first)).GetProperty("id").GetGuid());
        using var stale = NewClient(app);
        stale.DefaultRequestHeaders.Add("Cookie", priorCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await stale.GetAsync("/auth/current")).StatusCode);
        await WithDatabase(app, async db => Assert.Single(await db.ExternalIdentities.ToListAsync()));
        protocol.Subject = "different-subject";
        SetSession(first, await Complete(first, protocol, provider));
        Assert.NotEqual(firstProfile.GetProperty("id").GetGuid(), (await Current(first)).GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public async Task Administrator_email_collision_never_links_or_promotes_and_existing_admin_cookie_survives_failure(string verified)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        protocol.Email = "SSO-ADMIN@test.example";
        protocol.EmailVerified = verified;
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        await LocalLogin(client);
        var before = await Current(client);
        var result = await Complete(client, protocol, provider);
        Assert.EndsWith("/login?ssoError=email_collision", result.Headers.Location!.ToString());
        Assert.Equal(before.GetProperty("id").GetGuid(), (await Current(client)).GetProperty("id").GetGuid());
        await WithDatabase(app, async db =>
        {
            Assert.Empty(await db.ExternalIdentities.ToListAsync());
            Assert.Empty(await db.SsoSessions.ToListAsync());
            Assert.Equal(2, await db.Users.CountAsync());
        });
    }

    [Theory]
    [InlineData(null, "true", null, false)]
    [InlineData("not-an-email", "true", null, false)]
    [InlineData("person@test.example", null, "person@test.example", false)]
    [InlineData("person@test.example", "true", "person@test.example", true)]
    public async Task Optional_email_is_never_invented_or_implicitly_verified(
        string? claim, string? verified, string? expected, bool expectedVerification)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        protocol.Email = claim;
        protocol.EmailVerified = verified;
        protocol.Name = "invalid\nname";
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        SetSession(client, await Complete(client, protocol, provider));
        var profile = await Current(client);
        Assert.Equal(expected, profile.GetProperty("email").GetString());
        Assert.Equal(expectedVerification, profile.GetProperty("emailVerified").GetBoolean());
        Assert.Equal("SSO user", profile.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Reactivation_conservatively_revokes_existing_sessions_and_inflight_attempts()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        SetSession(client, await Complete(client, protocol, provider));
        var flow = await Start(client, protocol, provider);
        await WithDatabase(app, db => db.SsoProviders.Where(x => x.Id == provider)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TestedRevision, 1)));
        using var admin = NewClient(app);
        await LocalLogin(admin);
        (await admin.PostAsJsonAsync($"/auth/sso/providers/{provider}/activate", new { revision = 1 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/current")).StatusCode);
        var requests = protocol.TokenRequests;
        Assert.EndsWith("ssoError=login_failed", (await Callback(client, flow)).Headers.Location!.ToString());
        Assert.Equal(requests, protocol.TokenRequests);
    }

    [Fact]
    public async Task Switching_local_to_sso_and_back_revokes_the_old_cookie_in_the_single_cookie_slot()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        await LocalLogin(client);
        var adminCookie = client.DefaultRequestHeaders.GetValues("Cookie").Single();
        SetSession(client, await Complete(client, protocol, provider));
        Assert.Equal("User", (await Current(client)).GetProperty("role").GetString());
        var ssoCookie = client.DefaultRequestHeaders.GetValues("Cookie").Single();
        using var oldAdmin = NewClient(app);
        oldAdmin.DefaultRequestHeaders.Add("Cookie", adminCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await oldAdmin.GetAsync("/auth/current")).StatusCode);
        await LocalLogin(client);
        Assert.Equal("PlatformAdministrator", (await Current(client)).GetProperty("role").GetString());
        using var oldUser = NewClient(app);
        oldUser.DefaultRequestHeaders.Add("Cookie", ssoCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await oldUser.GetAsync("/auth/current")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/current")).StatusCode);
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("missing-nonce")]
    [InlineData("missing-subject")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    [InlineData("expiry")]
    [InlineData("correlation")]
    [InlineData("state")]
    [InlineData("denied")]
    [InlineData("expired-attempt")]
    [InlineData("disabled")]
    [InlineData("changed")]
    public async Task Invalid_or_stale_login_never_provisions(string failure)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        protocol.Failure = failure;
        if (failure == "missing-subject") protocol.Subject = "";
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        var flow = await Start(client, protocol, provider);
        if (failure == "correlation") flow = flow with { Cookies = "" };
        if (failure == "state") flow = flow with { Callback = Root + "/callback?state=invalid&code=code" };
        if (failure == "denied")
        {
            var query = QueryHelpers.ParseQuery(new Uri("http://localhost" + flow.Callback).Query);
            flow = flow with { Callback = QueryHelpers.AddQueryString(Root + "/callback", new Dictionary<string, string?>
                { ["state"] = query["state"], ["error"] = "access_denied", ["error_description"] = "secret-sensitive-provider-message" }) };
        }
        await WithDatabase(app, async db =>
        {
            if (failure == "disabled") await db.SsoProviders.ExecuteUpdateAsync(s => s.SetProperty(x => x.ActiveConfiguration, (string?)null));
            if (failure == "changed") await db.SsoProviders.ExecuteUpdateAsync(s => s.SetProperty(x => x.ActiveRevision, 2));
            if (failure == "expired-attempt") await db.SsoLoginAttempts.ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        });
        var result = await Callback(client, flow);
        Assert.Equal("http://localhost:4200/login?ssoError=login_failed", result.Headers.Location!.ToString());
        Assert.DoesNotContain("secret-sensitive", await result.Content.ReadAsStringAsync());
        Assert.DoesNotContain(LocalSessionAuthenticationHandler.CookieName, string.Join("", result.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : []));
        await WithDatabase(app, async db => Assert.Empty(await db.ExternalIdentities.ToListAsync()));
        var requests = protocol.TokenRequests;
        await Callback(client, flow);
        Assert.Equal(requests, protocol.TokenRequests);
    }

    [Fact]
    public async Task Editing_draft_does_not_affect_active_login_but_deactivation_and_account_disable_revoke_sessions()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        await WithDatabase(app, db => db.SsoProviders.ExecuteUpdateAsync(s => s.SetProperty(x => x.Authority, "https://wrong.auth0.com/").SetProperty(x => x.Revision, 2)));
        SetSession(client, await Complete(client, protocol, provider));
        var user = (await Current(client)).GetProperty("id").GetGuid();
        await WithDatabase(app, db => db.Users.Where(x => x.Id == user).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/teams")).StatusCode);
        await WithDatabase(app, db => db.Users.Where(x => x.Id == user).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true)));
        using var admin = NewClient(app);
        await LocalLogin(admin);
        (await admin.PostAsJsonAsync($"/auth/sso/providers/{provider}/deactivate", new { })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/current")).StatusCode);
        Assert.Equal("[]", await client.GetStringAsync(Root + "/providers"));
    }

    [Fact]
    public async Task Snapshot_change_during_exchange_fails_and_same_pair_can_login_through_another_active_provider()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        SetSession(client, await Complete(client, protocol, provider));
        var originalId = (await Current(client)).GetProperty("id").GetGuid();
        var otherProvider = await AddActiveProvider(app);
        SetSession(client, await Complete(client, protocol, otherProvider));
        Assert.Equal(originalId, (await Current(client)).GetProperty("id").GetGuid());
        protocol.PauseToken = true;
        var flow = await Start(client, protocol, provider);
        var completion = Callback(client, flow);
        await protocol.TokenEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try { await WithDatabase(app, db => db.SsoProviders.Where(x => x.Id == provider).ExecuteUpdateAsync(s => s.SetProperty(x => x.ActiveRevision, 2))); }
        finally { protocol.ReleaseToken.TrySetResult(true); }
        Assert.EndsWith("ssoError=login_failed", (await completion).Headers.Location!.ToString());
        await WithDatabase(app, async db => Assert.Single(await db.ExternalIdentities.ToListAsync()));
        protocol.PauseToken = false;
        protocol.Authority = "https://second.auth0.com/";
        var differentIssuer = await AddActiveProvider(app, protocol.Authority);
        SetSession(client, await Complete(client, protocol, differentIssuer));
        Assert.NotEqual(originalId, (await Current(client)).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Anonymous_start_requires_origin_protection_and_is_rate_limited()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        client.DefaultRequestHeaders.Remove("Origin");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"{Root}/{provider}/start", new { })).StatusCode);
        client.DefaultRequestHeaders.Add("Origin", "https://evil.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"{Root}/{provider}/start", new { })).StatusCode);
        client.DefaultRequestHeaders.Remove("Origin");
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
        client.DefaultRequestHeaders.Remove("X-Session-Protection");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"{Root}/{provider}/start", new { })).StatusCode);
        client.DefaultRequestHeaders.Add("X-Session-Protection", "1");
        var results = new List<HttpStatusCode>();
        for (var i = 0; i < 12; i++) results.Add((await client.PostAsJsonAsync($"{Root}/{provider}/start", new { })).StatusCode);
        Assert.Contains(HttpStatusCode.TooManyRequests, results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reactivation_during_code_exchange_cannot_restore_a_revoked_attempt(bool deactivateFirst)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        protocol.PauseToken = true;
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        var flow = await Start(client, protocol, provider);
        var completion = Callback(client, flow);
        await protocol.TokenEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            using var admin = NewClient(app);
            await LocalLogin(admin);
            await WithDatabase(app, db => db.SsoProviders.Where(x => x.Id == provider)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TestedRevision, 1)));
            if (deactivateFirst)
                (await admin.PostAsJsonAsync($"/auth/sso/providers/{provider}/deactivate", new { })).EnsureSuccessStatusCode();
            (await admin.PostAsJsonAsync($"/auth/sso/providers/{provider}/activate", new { revision = 1 })).EnsureSuccessStatusCode();
        }
        finally { protocol.ReleaseToken.TrySetResult(true); }

        Assert.EndsWith("ssoError=login_failed", (await completion).Headers.Location!.ToString());
        await WithDatabase(app, async db =>
        {
            Assert.Empty(await db.SsoSessions.ToListAsync());
            Assert.Empty(await db.ExternalIdentities.ToListAsync());
            Assert.Empty(await db.SsoLoginAttempts.ToListAsync());
        });
    }

    [Fact]
    public async Task Prefixed_public_login_callback_uses_browser_matching_protocol_cookie_paths()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        protocol.CallbackUrl = "http://localhost/api" + SsoLoginFlow.CallbackPath;
        using var app = Configure(factory, protocol, "http://localhost/api");
        var provider = await Activate(app);
        using var client = NewClient(app);
        var flow = await Start(client, protocol, provider);
        var jar = new CookieContainer();
        foreach (var header in flow.SetCookies) jar.SetCookies(new Uri("http://localhost/api" + Root + $"/{provider}/start"), header);
        Assert.Contains(".AspNetCore.Correlation.", jar.GetCookieHeader(new Uri(protocol.CallbackUrl)));
        Assert.Empty(jar.GetCookieHeader(new Uri("http://localhost/other")));
        var completion = await Callback(client, flow with { Cookies = jar.GetCookieHeader(new Uri(protocol.CallbackUrl)) });
        Assert.EndsWith("/dashboard", completion.Headers.Location!.ToString());
        foreach (var header in completion.Headers.GetValues("Set-Cookie")) jar.SetCookies(new Uri(protocol.CallbackUrl), header);
        Assert.DoesNotContain(".AspNetCore.", jar.GetCookieHeader(new Uri(protocol.CallbackUrl)));
        Assert.Contains(LocalSessionAuthenticationHandler.CookieName + "=s.", jar.GetCookieHeader(new Uri(protocol.CallbackUrl)));
    }

    private static FakeProtocol Protocol() => new() { CallbackUrl = "http://localhost" + SsoLoginFlow.CallbackPath };

    private static async Task<Guid> Activate(WebApplicationFactory<Program> app)
    {
        using var admin = NewClient(app);
        await LoginAsync(admin);
        return await AddActiveProvider(app);
    }

    private static async Task<Guid> AddActiveProvider(WebApplicationFactory<Program> app, string authority = "https://example.auth0.com/")
    {
        var id = Guid.NewGuid();
        await WithDatabase(app, async db =>
        {
            db.SsoProviders.Add(new SsoProvider
            {
                Id = id, Name = "Active provider", Type = "Auth0", Authority = authority, ClientId = "test-client",
                ActiveRevision = 1, ActiveConfiguration = JsonSerializer.Serialize(
                    new SsoActiveSnapshot("Auth0", "Active provider", authority, "test-client", null))
            });
            await db.SaveChangesAsync();
        });
        return id;
    }

    private sealed record Flow(string Callback, string Cookies, string[] SetCookies);

    private static async Task<Flow> Start(HttpClient client, FakeProtocol protocol, Guid provider)
    {
        var result = await client.PostAsJsonAsync($"{Root}/{provider}/start", new { });
        result.EnsureSuccessStatusCode();
        var query = QueryHelpers.ParseQuery(new Uri(JsonDocument.Parse(await result.Content.ReadAsStringAsync())
            .RootElement.GetProperty("authorizationUrl").GetString()!).Query);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal("openid profile email", query["scope"]);
        Assert.Equal(protocol.CallbackUrl, query["redirect_uri"]);
        var code = Guid.NewGuid().ToString("N");
        protocol.RegisterCode(code, query["nonce"].ToString(), query["code_challenge"].ToString());
        var cookies = result.Headers.GetValues("Set-Cookie").ToArray();
        return new Flow(QueryHelpers.AddQueryString(Root + "/callback", new Dictionary<string, string?>
            { ["state"] = query["state"], ["code"] = code }),
            string.Join("; ", cookies.Select(x => x.Split(';')[0])), cookies);
    }

    private static async Task<HttpResponseMessage> Complete(HttpClient client, FakeProtocol protocol, Guid id) =>
        await Callback(client, await Start(client, protocol, id));

    private static async Task<HttpResponseMessage> Callback(HttpClient client, Flow flow)
    {
        var existing = client.DefaultRequestHeaders.TryGetValues("Cookie", out var cookies) ? cookies.ToArray() : [];
        client.DefaultRequestHeaders.Remove("Cookie");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, flow.Callback);
            if (flow.Cookies.Length > 0) request.Headers.Add("Cookie", flow.Cookies);
            return await client.SendAsync(request);
        }
        finally { foreach (var cookie in existing) client.DefaultRequestHeaders.Add("Cookie", cookie); }
    }

    private static void SetSession(HttpClient client, HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Found);
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", response.Headers.GetValues("Set-Cookie")
            .Single(x => x.StartsWith(LocalSessionAuthenticationHandler.CookieName + "=")).Split(';')[0]);
    }

    private static async Task LocalLogin(HttpClient client)
    {
        var result = await client.PostAsJsonAsync("/auth/login", new { email = "sso-admin@test.example", password = "Strong-test-password-42!" });
        SetSession(client, result);
    }

    private static async Task<JsonElement> Current(HttpClient client) =>
        JsonDocument.Parse(await client.GetStringAsync("/auth/current")).RootElement;
}
