using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using static AiEngineeringManagerCopilot.IntegrationTests.Authentication.SsoEndpointsTests;
using static AiEngineeringManagerCopilot.IntegrationTests.Authentication.SsoLoginEndpointsTests;

namespace AiEngineeringManagerCopilot.IntegrationTests.Authentication;

public sealed class AccountLinkEndpointsTests
{
    private const string Root = "/auth/account/identities";
    private const string Password = "Strong-test-password-42!";

    [Fact]
    public async Task Migration_roundtrip_discards_unsupported_ephemeral_proofs_and_defaults_existing_approvals_to_false()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var local = NewClient(app);
        await LocalLogin(local);
        var userId = (await Current(local)).GetProperty("id").GetGuid();
        await WithDatabase(app, async db =>
        {
            db.ExternalIdentities.Add(new ExternalIdentity
            {
                Id = Guid.NewGuid(), UserId = userId, Issuer = protocol.Authority,
                Subject = protocol.Subject, AdministratorAccessApproved = true
            });
            db.SsoConnectionTests.Add(new SsoConnectionTest
            {
                Id = Guid.NewGuid(), ProviderId = provider, Revision = 1,
                SessionHash = new string('A', 64), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
            });
            var current = await db.SsoProviders.SingleAsync();
            db.SsoLoginAttempts.Add(new SsoLoginAttempt
            {
                Id = Guid.NewGuid(), ProviderId = provider, ActiveRevision = 1,
                ActiveConfiguration = current.ActiveConfiguration!, LinkTargetUserId = userId,
                LinkSourceSessionHash = new string('B', 64), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
            });
            await db.SaveChangesAsync();
            await db.Database.MigrateAsync("20261004100003_AddEffectiveSsoLogin");
            db.ChangeTracker.Clear();
            Assert.Empty(await db.SsoConnectionTests.ToListAsync());
            await db.Database.MigrateAsync();
            Assert.Empty(await db.SsoLoginAttempts.ToListAsync());
            Assert.False((await db.ExternalIdentities.SingleAsync()).AdministratorAccessApproved);
        });
    }

    [Fact]
    public async Task Local_credential_alone_does_not_approve_an_identity_and_explicit_link_rehashes_the_password()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var local = NewClient(app);
        await LocalLogin(local);
        var userId = (await Current(local)).GetProperty("id").GetGuid();
        string oldHash = "";
        await WithDatabase(app, async db =>
        {
            db.ExternalIdentities.Add(new ExternalIdentity
            {
                Id = Guid.NewGuid(), UserId = userId, Issuer = protocol.Authority, Subject = protocol.Subject
            });
            var admin = await db.LocalAdministrators.SingleAsync();
            admin.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<Microsoft.AspNetCore.Identity.IdentityUser>(
                Microsoft.Extensions.Options.Options.Create(new Microsoft.AspNetCore.Identity.PasswordHasherOptions { IterationCount = 1000 }))
                .HashPassword(new Microsoft.AspNetCore.Identity.IdentityUser(), Password);
            oldHash = admin.PasswordHash;
            await db.SaveChangesAsync();
        });
        using var sso = NewClient(app);
        Assert.EndsWith("ssoError=login_failed", (await Complete(sso, protocol, provider)).Headers.Location!.ToString());
        var completion = await Callback(local, await Start(local, protocol, provider, true));
        Assert.EndsWith("link=success", completion.Headers.Location!.ToString());
        await WithDatabase(app, async db =>
        {
            Assert.NotEqual(oldHash, (await db.LocalAdministrators.SingleAsync()).PasswordHash);
            Assert.True((await db.ExternalIdentities.SingleAsync()).AdministratorAccessApproved);
        });
        SetSession(sso, await Complete(sso, protocol, provider));
        Assert.Equal(userId, (await Current(sso)).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Concurrent_wrong_passwords_serialize_the_shared_lockout_counter()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var local = NewClient(app);
        await LocalLogin(local);
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => local.PostAsJsonAsync(Root + "/start",
            new { providerId = provider, password = "wrong", approveAdministratorAccess = true })));
        Assert.All(results, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        await WithDatabase(app, async db =>
        {
            var admin = await db.LocalAdministrators.SingleAsync();
            Assert.Equal(0, admin.FailedLoginCount);
            Assert.True(admin.LockoutUntil > DateTimeOffset.UtcNow);
            Assert.Empty(await db.SsoLoginAttempts.ToListAsync());
        });
        (await local.GetAsync("/auth/current")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Explicit_proof_preserves_local_account_and_owner_data_then_allows_SSO_administration_and_revocable_unlink()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        protocol.Email = "sso-admin@test.example";
        protocol.Name = "Must not overwrite local name";
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var local = NewClient(app);
        await LocalLogin(local);
        var before = await Current(local);
        var userId = before.GetProperty("id").GetGuid();
        var localCookie = local.DefaultRequestHeaders.GetValues("Cookie").Single();
        (await local.PostAsJsonAsync("/teams", new { name = "Existing owner team" })).EnsureSuccessStatusCode();
        var flow = await Start(local, protocol, provider, link: true);
        var completion = await Callback(local, flow); // Only Lax protocol cookies, never the Strict local cookie.
        Assert.EndsWith("/account/security?link=success", completion.Headers.Location!.ToString());
        Assert.DoesNotContain(LocalSessionAuthenticationHandler.CookieName,
            string.Join("", completion.Headers.GetValues("Set-Cookie")));
        Assert.Equal(localCookie, local.DefaultRequestHeaders.GetValues("Cookie").Single());
        Assert.Equal(before.GetRawText(), (await Current(local)).GetRawText());
        Assert.True(protocol.PkceVerified);
        var list = JsonDocument.Parse(await local.GetStringAsync(Root)).RootElement;
        Assert.True(list.GetProperty("canLink").GetBoolean());
        var identityId = list.GetProperty("identities")[0].GetProperty("id").GetGuid();
        Assert.DoesNotContain(protocol.Subject, list.GetRawText());
        // Idempotent after repeating BOTH proofs.
        Assert.EndsWith("link=success", (await Callback(local, await Start(local, protocol, provider, true))).Headers.Location!.ToString());
        using var sso = NewClient(app);
        SetSession(sso, await Complete(sso, protocol, provider));
        var profile = await Current(sso);
        Assert.Equal(userId, profile.GetProperty("id").GetGuid());
        Assert.Equal("SSO admin", profile.GetProperty("name").GetString());
        Assert.Equal("PlatformAdministrator", profile.GetProperty("role").GetString());
        Assert.Single(JsonDocument.Parse(await sso.GetStringAsync("/teams")).RootElement.EnumerateArray());
        Assert.False(JsonDocument.Parse(await sso.GetStringAsync(Root)).RootElement.GetProperty("canLink").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await sso.PostAsJsonAsync(Root + "/start",
            new { providerId = provider, password = Password, approveAdministratorAccess = true })).StatusCode);

        var draft = await sso.PostAsJsonAsync("/auth/sso/providers", new
        {
            type = "Auth0", name = "New draft", tenant = "example.auth0.com", clientId = "test-client",
            secretAction = "clear"
        });
        draft.EnsureSuccessStatusCode();
        var draftId = JsonDocument.Parse(await draft.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        (await sso.GetAsync("/auth/sso/providers")).EnsureSuccessStatusCode();
        (await sso.PutAsJsonAsync($"/auth/sso/providers/{draftId}", new
        {
            revision = 1, type = "Auth0", name = "Edited draft", tenant = "example.auth0.com",
            clientId = "test-client", secretAction = "retain"
        })).EnsureSuccessStatusCode();
        protocol.CallbackUrl = "http://localhost/auth/sso/callback";
        var test = await sso.PostAsJsonAsync($"/auth/sso/providers/{draftId}/test", new { revision = 2 });
        test.EnsureSuccessStatusCode();
        var query = QueryHelpers.ParseQuery(new Uri(JsonDocument.Parse(await test.Content.ReadAsStringAsync())
            .RootElement.GetProperty("authorizationUrl").GetString()!).Query);
        protocol.RegisterCode("admin-test", query["nonce"].ToString(), query["code_challenge"].ToString());
        var callback = QueryHelpers.AddQueryString("/auth/sso/callback", new Dictionary<string, string?>
            { ["state"] = query["state"], ["code"] = "admin-test" });
        var testFlow = new Flow(callback, string.Join("; ", test.Headers.GetValues("Set-Cookie").Select(x => x.Split(';')[0])), []);
        Assert.EndsWith("ssoTest=success", (await Callback(sso, testFlow)).Headers.Location!.ToString());
        (await sso.PostAsJsonAsync($"/auth/sso/providers/{draftId}/activate", new { revision = 2 })).EnsureSuccessStatusCode();
        (await sso.PostAsJsonAsync($"/auth/sso/providers/{draftId}/deactivate", new { })).EnsureSuccessStatusCode();
        (await sso.DeleteAsync($"/auth/sso/providers/{draftId}")).EnsureSuccessStatusCode();
        var pending = await Start(local, ProtocolForLogin(protocol), provider, true);
        Assert.Equal(HttpStatusCode.Unauthorized, (await local.PostAsJsonAsync($"{Root}/{identityId}/unlink",
            new { password = "wrong" })).StatusCode);
        (await sso.GetAsync("/auth/current")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await local.PostAsJsonAsync($"{Root}/{identityId}/unlink",
            new { password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await sso.GetAsync("/auth/current")).StatusCode);
        Assert.DoesNotContain("link=success", (await Callback(local, pending)).Headers.Location!.ToString());
        Assert.Equal(userId, (await Current(local)).GetProperty("id").GetGuid());
        await WithDatabase(app, async db =>
        {
            Assert.Empty(await db.ExternalIdentities.ToListAsync());
            Assert.Empty(await db.SsoSessions.ToListAsync());
            Assert.Single(await db.LocalAdministrators.ToListAsync());
            Assert.Equal(2, await db.Users.CountAsync());
        });
        await LocalLogin(sso);
        Assert.Equal(userId, (await Current(sso)).GetProperty("id").GetGuid());
    }

    private static FakeProtocol ProtocolForLogin(FakeProtocol protocol)
    {
        protocol.CallbackUrl = "http://localhost/auth/sso/login/callback";
        return protocol;
    }

    [Fact]
    public async Task Password_consent_and_CSRF_required_and_lockout_is_shared_without_invalidating_local_session()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var client = NewClient(app);
        await LocalLogin(client);
        var request = new { providerId = provider, password = Password, approveAdministratorAccess = true };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Root + "/start",
            new { providerId = provider, password = Password, approveAdministratorAccess = false })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Session-Protection");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Root + "/start", request)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Session-Protection", "1");
        client.DefaultRequestHeaders.Remove("Origin");
        client.DefaultRequestHeaders.Add("Origin", "https://attacker.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Root + "/start", request)).StatusCode);
        client.DefaultRequestHeaders.Remove("Origin");
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Root + "/start",
                new { providerId = provider, password = "wrong", approveAdministratorAccess = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Root + "/start", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/auth/login",
            new { email = "sso-admin@test.example", password = Password })).StatusCode);
        (await client.GetAsync("/auth/current")).EnsureSuccessStatusCode();
        await WithDatabase(app, async db =>
        {
            Assert.True((await db.LocalAdministrators.SingleAsync()).LockoutUntil > DateTimeOffset.UtcNow);
            Assert.Empty(await db.SsoLoginAttempts.ToListAsync());
            Assert.Empty(await db.ExternalIdentities.ToListAsync());
        });
    }

    [Fact]
    public async Task Anonymous_and_ordinary_SSO_cannot_link_and_identity_owned_by_another_user_cannot_merge()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var anonymous = NewClient(app);
        var request = new { providerId = provider, password = Password, approveAdministratorAccess = true };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(Root + "/start", request)).StatusCode);
        SetSession(anonymous, await Complete(anonymous, protocol, provider));
        var ordinaryId = (await Current(anonymous)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await anonymous.PostAsJsonAsync(Root + "/start", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await anonymous.GetAsync(Root)).StatusCode);
        using var local = NewClient(app);
        await LocalLogin(local);
        var flow = await Start(local, protocol, provider, true);
        Assert.EndsWith("link=identity_conflict", (await Callback(local, flow)).Headers.Location!.ToString());
        await WithDatabase(app, async db =>
        {
            var identity = await db.ExternalIdentities.SingleAsync();
            Assert.Equal(ordinaryId, identity.UserId);
            Assert.False(identity.AdministratorAccessApproved);
            Assert.Equal(3, await db.Users.CountAsync());
        });
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("missing-auth-time")]
    [InlineData("stale-auth-time")]
    [InlineData("future-auth-time")]
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
    [InlineData("expired-session")]
    [InlineData("logout")]
    [InlineData("rotation")]
    [InlineData("credential")]
    [InlineData("user")]
    [InlineData("role")]
    [InlineData("provider")]
    public async Task Invalid_proofs_or_revoked_sources_never_link(string failure)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        protocol.Failure = failure;
        if (failure == "missing-subject") protocol.Subject = "";
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var local = NewClient(app);
        await LocalLogin(local);
        var flow = await Start(local, protocol, provider, true);
        if (failure == "correlation") flow = flow with { Cookies = "" };
        if (failure == "state") flow = flow with { Callback = "/auth/sso/login/callback?state=invalid&code=code" };
        if (failure == "denied")
        {
            var query = QueryHelpers.ParseQuery(new Uri("http://localhost" + flow.Callback).Query);
            flow = flow with { Callback = QueryHelpers.AddQueryString("/auth/sso/login/callback",
                new Dictionary<string, string?> { ["state"] = query["state"], ["error"] = "access_denied" }) };
        }
        if (failure == "logout") (await local.PostAsJsonAsync("/auth/logout", new { })).EnsureSuccessStatusCode();
        if (failure == "rotation") await LocalLogin(local);
        await WithDatabase(app, async db =>
        {
            if (failure == "expired-attempt") await db.SsoLoginAttempts.ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
            if (failure == "expired-session") await db.AdministratorSessions.ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
            if (failure == "credential") await db.LocalAdministrators.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
            if (failure == "role") await db.LocalAdministrators.ExecuteUpdateAsync(s => s.SetProperty(x => x.Role, "User"));
            if (failure == "user") await db.Users.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
            if (failure == "provider") await db.SsoProviders.ExecuteUpdateAsync(s => s.SetProperty(x => x.ActiveRevision, 2));
        });
        var completion = await Callback(local, flow);
        Assert.DoesNotContain("link=success", completion.Headers.Location!.ToString());
        Assert.DoesNotContain(LocalSessionAuthenticationHandler.CookieName,
            string.Join("", completion.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : []));
        var count = protocol.TokenRequests;
        await Callback(local, flow);
        Assert.Equal(count, protocol.TokenRequests);
        await WithDatabase(app, async db =>
        {
            Assert.Empty(await db.ExternalIdentities.ToListAsync());
            Assert.Empty(await db.SsoSessions.ToListAsync());
            Assert.Equal(2, await db.Users.CountAsync());
        });
    }

    [Theory]
    [InlineData("logout")]
    [InlineData("credential")]
    [InlineData("user")]
    [InlineData("reactivate")]
    public async Task Source_or_snapshot_change_during_token_exchange_invalidates_consumed_link(string change)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var local = NewClient(app);
        await LocalLogin(local);
        protocol.PauseToken = true;
        var flow = await Start(local, protocol, provider, true);
        using var mutation = NewClient(app);
        mutation.DefaultRequestHeaders.Add("Cookie", local.DefaultRequestHeaders.GetValues("Cookie").Single());
        var completion = Callback(local, flow);
        await protocol.TokenEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            if (change == "logout") (await mutation.PostAsJsonAsync("/auth/logout", new { })).EnsureSuccessStatusCode();
            await WithDatabase(app, async db =>
            {
                if (change == "credential") await db.LocalAdministrators.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
                if (change == "user") await db.Users.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
                if (change == "reactivate") await db.SsoProviders.ExecuteUpdateAsync(s => s.SetProperty(x => x.TestedRevision, 1));
            });
            if (change == "reactivate")
                (await mutation.PostAsJsonAsync($"/auth/sso/providers/{provider}/activate", new { revision = 1 })).EnsureSuccessStatusCode();
        }
        finally { protocol.ReleaseToken.TrySetResult(true); }
        Assert.DoesNotContain("link=success", (await completion).Headers.Location!.ToString());
        await WithDatabase(app, async db => Assert.Empty(await db.ExternalIdentities.ToListAsync()));
    }

    [Fact]
    public async Task Concurrent_links_are_idempotent_and_draft_changes_do_not_replace_the_active_snapshot()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var first = NewClient(app);
        using var second = NewClient(app);
        await LocalLogin(first);
        await LocalLogin(second);
        var a = await Start(first, protocol, provider, true);
        var b = await Start(second, protocol, provider, true);
        await WithDatabase(app, db => db.SsoProviders.ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.Authority, "https://wrong.auth0.com/").SetProperty(x => x.Revision, 2)));
        var results = await Task.WhenAll(Callback(first, a), Callback(second, b));
        Assert.All(results, x => Assert.EndsWith("link=success", x.Headers.Location!.ToString()));
        await WithDatabase(app, async db => Assert.Single(await db.ExternalIdentities.ToListAsync()));
    }

    [Theory]
    [InlineData("approval")]
    [InlineData("credential")]
    [InlineData("user")]
    [InlineData("role")]
    public async Task Linked_SSO_admin_requests_revalidate_approval_credential_user_and_role(string change)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var local = NewClient(app);
        await LocalLogin(local);
        await Callback(local, await Start(local, protocol, provider, true));
        using var sso = NewClient(app);
        SetSession(sso, await Complete(sso, protocol, provider));
        (await sso.GetAsync("/auth/sso/providers")).EnsureSuccessStatusCode();
        await WithDatabase(app, async db =>
        {
            if (change == "approval") await db.ExternalIdentities.ExecuteUpdateAsync(s => s.SetProperty(x => x.AdministratorAccessApproved, false));
            if (change == "credential") await db.LocalAdministrators.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
            if (change == "user") await db.Users.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
            if (change == "role") await db.LocalAdministrators.ExecuteUpdateAsync(s => s.SetProperty(x => x.Role, "User"));
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await sso.GetAsync("/auth/current")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await sso.GetAsync("/auth/sso/providers")).StatusCode);
        Assert.EndsWith("ssoError=login_failed", (await Complete(sso, protocol, provider)).Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("logout")]
    [InlineData("approval")]
    public async Task Approved_SSO_connection_test_cannot_complete_after_its_source_proof_is_revoked(string change)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = Protocol();
        using var app = Configure(factory, protocol);
        var provider = await Activate(app);
        using var local = NewClient(app);
        await LocalLogin(local);
        Assert.EndsWith("link=success", (await Callback(local, await Start(local, protocol, provider, true))).Headers.Location!.ToString());
        using var sso = NewClient(app);
        SetSession(sso, await Complete(sso, protocol, provider));
        using var mutation = NewClient(app);
        mutation.DefaultRequestHeaders.Add("Cookie", sso.DefaultRequestHeaders.GetValues("Cookie").Single());
        protocol.CallbackUrl = "http://localhost/auth/sso/callback";
        var start = await sso.PostAsJsonAsync($"/auth/sso/providers/{provider}/test", new { revision = 1 });
        start.EnsureSuccessStatusCode();
        var query = QueryHelpers.ParseQuery(new Uri(JsonDocument.Parse(await start.Content.ReadAsStringAsync())
            .RootElement.GetProperty("authorizationUrl").GetString()!).Query);
        protocol.RegisterCode("test-source", query["nonce"].ToString(), query["code_challenge"].ToString());
        var flow = new Flow(QueryHelpers.AddQueryString("/auth/sso/callback",
            new Dictionary<string, string?> { ["state"] = query["state"], ["code"] = "test-source" }),
            string.Join("; ", start.Headers.GetValues("Set-Cookie").Select(x => x.Split(';')[0])), []);
        protocol.PauseToken = true;
        var completion = Callback(sso, flow);
        await protocol.TokenEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            if (change == "logout") (await mutation.PostAsJsonAsync("/auth/logout", new { })).EnsureSuccessStatusCode();
            else await WithDatabase(app, db => db.ExternalIdentities.ExecuteUpdateAsync(
                s => s.SetProperty(x => x.AdministratorAccessApproved, false)));
        }
        finally { protocol.ReleaseToken.TrySetResult(true); }
        Assert.EndsWith("ssoTest=failed", (await completion).Headers.Location!.ToString());
        await WithDatabase(app, async db => Assert.Null((await db.SsoProviders.SingleAsync()).TestedRevision));
    }
}
