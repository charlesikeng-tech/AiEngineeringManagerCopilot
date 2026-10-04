using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Api.Authentication.Sso;
using AiEngineeringManagerCopilot.Api.Endpoints;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AiEngineeringManagerCopilot.IntegrationTests.Authentication;

public sealed class SsoEndpointsTests
{
    private const string Root = "/auth/sso/providers";
    private static SsoEndpoints.DraftRequest Draft(int? revision = null, string action = "replace", string? secret = "test-secret-only") =>
        new(revision, "Auth0", "Test tenant", "example.auth0.com", null, "test-client", action, secret);

    [Fact]
    public async Task Configuration_requires_real_admin_session_and_csrf_and_redacts_secret()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Authentication:Sso:PublicApiBaseUrl", "http://localhost");
            builder.UseSetting("Authentication:Sso:FrontendOrigin", "http://localhost:4200");
        });
        using var client = app.CreateClient(new() { HandleCookies = false, AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Root)).StatusCode);
        var bearer = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "LocalAuthTests", Audience = "LocalAuthTests", Expires = DateTime.UtcNow.AddMinutes(5),
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Role, "PlatformAdministrator")]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(LocalAuthenticationWebApplicationFactory.JwtKey)), SecurityAlgorithms.HmacSha256)
        });
        client.DefaultRequestHeaders.Authorization = new("Bearer", bearer);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Root)).StatusCode);
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
        client.DefaultRequestHeaders.Add("X-Session-Protection", "1");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/auth/account/identities/start",
            new { providerId = Guid.NewGuid(), password = "Strong-test-password-42!", approveAdministratorAccess = true })).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client);
        client.DefaultRequestHeaders.Remove("X-Session-Protection");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Root, Draft())).StatusCode);
        client.DefaultRequestHeaders.Add("X-Session-Protection", "1");
        var saved = await client.PostAsJsonAsync(Root, Draft());
        saved.EnsureSuccessStatusCode();
        var json = await saved.Content.ReadAsStringAsync();
        Assert.DoesNotContain("test-secret-only", json);
        Assert.DoesNotContain("protectedSecret", json);
        Assert.Contains("\"hasSecret\":true", json);
        var id = JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{Root}/{id}/activate", new { revision = 1 })).StatusCode);
        var retained = await client.PutAsJsonAsync($"{Root}/{id}", Draft(1, "retain", null));
        retained.EnsureSuccessStatusCode();
        Assert.Contains("\"hasSecret\":true", await retained.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"{Root}/{id}", Draft(1))).StatusCode);
        var cleared = await client.PutAsJsonAsync($"{Root}/{id}", Draft(2, "clear", null));
        Assert.Contains("\"hasSecret\":false", await cleared.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Signed_protocol_flow_exchanges_pkce_code_and_preserves_active_snapshot_on_edit()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = new FakeProtocol();
        using var app = Configure(factory, protocol);
        using var client = NewClient(app);
        await LoginAsync(client);
        var id = await SaveAsync(client);
        var (callback, cookie) = await StartAsync(client, protocol, id, 1);
        var callbackResponse = await CallbackAsync(client, callback, cookie);
        Assert.EndsWith("?ssoTest=success", callbackResponse.Headers.Location!.ToString());
        Assert.Equal(1, protocol.TokenRequests);
        Assert.True(protocol.PkceVerified);
        Assert.DoesNotContain("aem.admin.session=", string.Join("", callbackResponse.Headers.TryGetValues("Set-Cookie", out var setCookies) ? setCookies : []));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Root}/{id}/activate", new { revision = 1 })).StatusCode);
        await client.PutAsJsonAsync($"{Root}/{id}", Draft(1, "retain", null) with { Name = "Edited draft" });
        var providers = JsonDocument.Parse(await client.GetStringAsync(Root)).RootElement.GetProperty("providers");
        var provider = providers[0];
        Assert.Equal(1, provider.GetProperty("activeRevision").GetInt32());
        Assert.Equal("Test tenant", provider.GetProperty("active").GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, provider.GetProperty("testedRevision").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{Root}/{id}/activate", new { revision = 2 })).StatusCode);
        protocol.Failure = "nonce";
        var (failedCallback, failedCookies) = await StartAsync(client, protocol, id, 2);
        Assert.EndsWith("?ssoTest=failed", (await CallbackAsync(client, failedCallback, failedCookies)).Headers.Location!.ToString());
        await WithDatabase(app, async db =>
        {
            var persisted = await db.SsoProviders.SingleAsync();
            Assert.Equal(1, persisted.ActiveRevision);
            Assert.DoesNotContain("test-secret-only", persisted.ActiveConfiguration!);
            Assert.Null(persisted.TestedRevision);
            Assert.Equal(2, await db.Users.CountAsync());
            Assert.Equal(1, await db.AdministratorSessions.CountAsync());
        });
        await CallbackAsync(client, callback, cookie);
        Assert.Equal(2, protocol.TokenRequests);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Root}/{id}/deactivate", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{Root}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/auth/current")).StatusCode);
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    [InlineData("expiry")]
    [InlineData("correlation")]
    [InlineData("state")]
    [InlineData("revision")]
    [InlineData("expired-state")]
    [InlineData("logout")]
    [InlineData("inactive-admin")]
    [InlineData("superseded")]
    [InlineData("denied")]
    [InlineData("expired-session")]
    public async Task Failed_or_unbound_protocol_never_verifies_a_draft(string failure)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = new FakeProtocol { Failure = failure };
        using var app = Configure(factory, protocol);
        using var client = NewClient(app);
        await LoginAsync(client);
        var id = await SaveAsync(client);
        var (callback, cookies) = await StartAsync(client, protocol, id, 1);
        if (failure == "revision") await client.PutAsJsonAsync($"{Root}/{id}", Draft(1));
        if (failure == "correlation") cookies = "";
        if (failure == "state") callback = "/auth/sso/callback?code=test-code&state=invalid";
        if (failure == "logout") await client.PostAsJsonAsync("/auth/logout", new { });
        if (failure == "expired-state")
            await WithDatabase(app, db => db.SsoConnectionTests.ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1))));
        if (failure == "expired-session")
            await WithDatabase(app, db => db.AdministratorSessions.ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1))));
        if (failure == "denied")
        {
            var state = QueryHelpers.ParseQuery(new Uri("http://localhost" + callback).Query)["state"].ToString();
            callback = QueryHelpers.AddQueryString("/auth/sso/callback", new Dictionary<string, string?>
            { ["state"] = state, ["error"] = "access_denied", ["error_description"] = "Sensitive provider error" });
        }
        if (failure == "inactive-admin")
            await WithDatabase(app, db => db.LocalAdministrators.ExecuteUpdateAsync(s => s.SetProperty(a => a.IsActive, false)));
        if (failure == "superseded") await StartAsync(client, protocol, id, 1);
        var response = await CallbackAsync(client, callback, cookies);
        Assert.EndsWith("?ssoTest=failed", response.Headers.Location!.ToString());
        await WithDatabase(app, async db => Assert.Null((await db.SsoProviders.SingleAsync()).TestedRevision));
        if (failure is "state" or "revision" or "expired-state" or "logout" or "inactive-admin" or "superseded" or "correlation" or "denied" or "expired-session")
            Assert.Equal(0, protocol.TokenRequests);
    }

    [Fact]
    public async Task Concurrent_edit_during_code_exchange_cannot_verify_the_new_revision()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = new FakeProtocol { PauseToken = true };
        using var app = Configure(factory, protocol);
        using var client = NewClient(app);
        await LoginAsync(client);
        var id = await SaveAsync(client);
        var (callback, cookies) = await StartAsync(client, protocol, id, 1);
        using var editor = NewClient(app);
        editor.DefaultRequestHeaders.Add("Cookie", client.DefaultRequestHeaders.GetValues("Cookie").Single());
        var completion = CallbackAsync(client, callback, cookies);
        await protocol.TokenEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            var edit = await editor.PutAsJsonAsync($"{Root}/{id}", Draft(1, "retain", null) with { Name = "Concurrent edit" });
            edit.EnsureSuccessStatusCode();
        }
        finally { protocol.ReleaseToken.TrySetResult(true); }
        var result = await completion;
        Assert.EndsWith("?ssoTest=failed", result.Headers.Location!.ToString());
        await WithDatabase(app, async db =>
        {
            var provider = await db.SsoProviders.SingleAsync();
            Assert.Equal(2, provider.Revision);
            Assert.Null(provider.TestedRevision);
            Assert.Null(provider.LastTestError);
        });
    }

    [Fact]
    public async Task Prefixed_public_callback_sends_and_deletes_protocol_cookies_with_browser_path_matching()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var protocol = new FakeProtocol { CallbackUrl = "http://localhost/api/auth/sso/callback" };
        using var app = Configure(factory, protocol, "http://localhost/api");
        using var client = NewClient(app);
        await LoginAsync(client);
        var id = await SaveAsync(client);
        var response = await client.PostAsJsonAsync($"{Root}/{id}/test", new { revision = 1 });
        response.EnsureSuccessStatusCode();
        var url = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("authorizationUrl").GetString()!;
        var query = QueryHelpers.ParseQuery(new Uri(url).Query);
        Assert.Equal(protocol.CallbackUrl, query["redirect_uri"]);
        protocol.Nonce = query["nonce"].ToString();
        protocol.Challenge = query["code_challenge"].ToString();

        var cookies = new CookieContainer();
        foreach (var header in response.Headers.GetValues("Set-Cookie"))
            cookies.SetCookies(new Uri($"http://localhost/api{Root}/{id}/test"), header);
        var publicCallback = new Uri(protocol.CallbackUrl);
        var callbackCookies = cookies.GetCookieHeader(publicCallback);
        Assert.Contains(".AspNetCore.Correlation.", callbackCookies);
        Assert.Contains(".AspNetCore.OpenIdConnect.Nonce.", callbackCookies);
        Assert.Empty(cookies.GetCookieHeader(new Uri("http://localhost/other")));

        // Simulate a proxy stripping /api after the browser has selected cookies.
        var callback = QueryHelpers.AddQueryString("/auth/sso/callback", new Dictionary<string, string?>
        { ["state"] = query["state"], ["code"] = "test-code" });
        var completion = await CallbackAsync(client, callback, callbackCookies);
        Assert.EndsWith("?ssoTest=success", completion.Headers.Location!.ToString());
        Assert.True(protocol.PkceVerified);
        foreach (var header in completion.Headers.GetValues("Set-Cookie"))
            cookies.SetCookies(publicCallback, header);
        Assert.Empty(cookies.GetCookieHeader(publicCallback));
    }

    internal static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Configure(
        LocalAuthenticationWebApplicationFactory factory, FakeProtocol protocol, string publicApiBaseUrl = "http://localhost") =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Authentication:Sso:PublicApiBaseUrl", publicApiBaseUrl);
            builder.UseSetting("Authentication:Sso:FrontendOrigin", "http://localhost:4200");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<SsoProtocolHttpClientFactory>();
                services.AddSingleton<SsoProtocolHttpClientFactory>(protocol);
            });
        });

    internal static HttpClient NewClient(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> app)
    {
        var client = app.CreateClient(new() { HandleCookies = false, AllowAutoRedirect = false, BaseAddress = new Uri("http://localhost") });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
        client.DefaultRequestHeaders.Add("X-Session-Protection", "1");
        return client;
    }

    internal static async Task WithDatabase(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> app, Func<AppDbContext, Task> action)
    {
        using var scope = app.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    internal static async Task LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/auth/setup", new
        {
            secret = LocalAuthenticationWebApplicationFactory.SetupSecret, email = "sso-admin@test.example",
            name = "SSO admin", password = "Strong-test-password-42!"
        });
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith(LocalSessionAuthenticationHandler.CookieName)).Split(';')[0]);
    }

    private static async Task<Guid> SaveAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(Root, Draft());
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<(string Callback, string Cookies)> StartAsync(HttpClient client, FakeProtocol protocol, Guid id, int revision)
    {
        var response = await client.PostAsJsonAsync($"{Root}/{id}/test", new { revision });
        response.EnsureSuccessStatusCode();
        var url = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("authorizationUrl").GetString()!;
        var query = QueryHelpers.ParseQuery(new Uri(url).Query);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal("http://localhost/auth/sso/callback", query["redirect_uri"]);
        protocol.Nonce = query["nonce"].ToString();
        protocol.Challenge = query["code_challenge"].ToString();
        return (QueryHelpers.AddQueryString("/auth/sso/callback", new Dictionary<string, string?>
        { ["state"] = query["state"], ["code"] = "test-code" }),
            string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(x => x.Split(';')[0])));
    }

    private static async Task<HttpResponseMessage> CallbackAsync(HttpClient client, string callback, string cookie)
    {
        // The local Strict cookie is absent on the provider's cross-site redirect.
        var original = client.DefaultRequestHeaders.GetValues("Cookie").Single();
        client.DefaultRequestHeaders.Remove("Cookie");
        using var request = new HttpRequestMessage(HttpMethod.Get, callback);
        if (cookie != "") request.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(request);
        client.DefaultRequestHeaders.Add("Cookie", original);
        return response;
    }

    internal sealed class FakeProtocol : SsoProtocolHttpClientFactory, IDisposable
    {
        private readonly RSA rsa = RSA.Create(2048);
        public string Nonce = "";
        public string Challenge = "";
        public string Failure = "";
        public string CallbackUrl = "http://localhost/auth/sso/callback";
        public string Authority = "https://example.auth0.com/";
        public string Subject = "external-test-user";
        public string? Email;
        public string? EmailVerified;
        public string? Name;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Nonce, string Challenge)> codes = new();
        public void RegisterCode(string code, string nonce, string challenge) => codes[code] = (nonce, challenge);
        public int TokenRequests;
        public bool PkceVerified;
        public bool AdvertisePar;
        public bool RequirePar;
        public int ParRequests;
        public bool PauseToken;
        public TaskCompletionSource<bool> TokenEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ReleaseToken = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override HttpClient Create(string authority) => new(new ProtocolHandler(this));
        public void Dispose() => rsa.Dispose();
        private sealed class ProtocolHandler(FakeProtocol owner) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                object body;
                var key = new RsaSecurityKey(owner.rsa) { KeyId = "test-key" };
                if (owner.Failure == "discovery-unavailable")
                    throw new HttpRequestException("sensitive-provider-response", null, HttpStatusCode.ServiceUnavailable);
                if (request.RequestUri!.AbsolutePath.Contains(".well-known"))
                    body = new
                    {
                        issuer = owner.Authority,
                        authorization_endpoint = owner.Authority.TrimEnd('/') + "/authorize",
                        token_endpoint = owner.Authority.TrimEnd('/') + "/token",
                        pushed_authorization_request_endpoint = owner.AdvertisePar ? owner.Authority.TrimEnd('/') + "/par" : null,
                        require_pushed_authorization_requests = owner.RequirePar,
                        jwks_uri = owner.Authority.TrimEnd('/') + "/keys",
                        response_types_supported = new[] { "code" }, subject_types_supported = new[] { "public" },
                        id_token_signing_alg_values_supported = new[] { "RS256" }
                    };
                else if (request.RequestUri.AbsolutePath == "/par")
                {
                    owner.ParRequests++;
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = JsonContent.Create(new { error = "invalid_request", error_description = "sensitive-provider-response" })
                    };
                }
                else if (request.RequestUri.AbsolutePath == "/keys")
                {
                    var publicKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(owner.rsa.ExportParameters(false)) { KeyId = key.KeyId });
                    body = new { keys = new[] { publicKey } };
                }
                else
                {
                    owner.TokenRequests++;
                    if (owner.PauseToken)
                    {
                        owner.TokenEntered.TrySetResult(true);
                        await owner.ReleaseToken.Task.WaitAsync(cancellationToken);
                    }
                    var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(cancellationToken));
                    var flow = owner.codes.TryGetValue(form["code"].ToString(), out var registered) ? registered : (owner.Nonce, owner.Challenge);
                    owner.PkceVerified = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(form["code_verifier"].ToString()))) == flow.Item2;
                    Assert.Equal("authorization_code", form["grant_type"]);
                    Assert.Equal(owner.CallbackUrl, form["redirect_uri"]);
                    var claims = new List<Claim> { new("sub", owner.Subject) };
                    if (owner.Failure != "missing-auth-time")
                        claims.Add(new Claim("auth_time",
                            (owner.Failure == "stale-auth-time" ? DateTimeOffset.UtcNow.AddHours(-1) :
                                owner.Failure == "future-auth-time" ? DateTimeOffset.UtcNow.AddHours(1) : DateTimeOffset.UtcNow)
                                .ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ClaimValueTypes.Integer64));
                    if (owner.Failure != "missing-nonce") claims.Add(new("nonce", owner.Failure == "nonce" ? "wrong" : flow.Item1));
                    if (owner.Email is not null) claims.Add(new("email", owner.Email));
                    if (owner.EmailVerified is not null) claims.Add(new("email_verified", owner.EmailVerified));
                    if (owner.Name is not null) claims.Add(new("name", owner.Name));
                    claims.Add(new("role", "PlatformAdministrator"));
                    var descriptor = new SecurityTokenDescriptor
                    {
                        Issuer = owner.Failure == "issuer" ? "https://other.auth0.com/" : owner.Authority,
                        Audience = owner.Failure == "audience" ? "wrong-client" : "test-client",
                        Subject = new ClaimsIdentity(claims),
                        IssuedAt = DateTime.UtcNow.AddMinutes(-10),
                        NotBefore = DateTime.UtcNow.AddMinutes(-10),
                        Expires = owner.Failure == "expiry" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5),
                        SigningCredentials = new SigningCredentials(owner.Failure == "signature" ? new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key" } : key, SecurityAlgorithms.RsaSha256)
                    };
                    body = new { access_token = "fake-access-token", token_type = "Bearer", expires_in = 300, id_token = new JsonWebTokenHandler().CreateToken(descriptor) };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
            }
        }
    }
}
