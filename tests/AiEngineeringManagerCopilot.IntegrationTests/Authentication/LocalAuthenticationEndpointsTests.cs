using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AiEngineeringManagerCopilot.IntegrationTests.Authentication;

public sealed class LocalAuthenticationEndpointsTests
{
    private const string Email = "administrator@test.example";
    private const string Password = "Secure-Test-Password-2026!";

    private static Task<HttpResponseMessage> Setup(HttpClient client, string email = Email, string? secret = null, string password = Password) =>
        client.PostAsJsonAsync("/auth/setup", new
        {
            secret = secret ?? LocalAuthenticationWebApplicationFactory.SetupSecret,
            email, name = "Administrator", password, role = "IgnoredPublicRole"
        });

    private static Task<HttpResponseMessage> Login(HttpClient client, string password = Password) =>
        client.PostAsJsonAsync("/auth/login", new { email = Email, password });

    private static string Cookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

    private static async Task<bool> Available(HttpClient client) =>
        (await client.GetFromJsonAsync<SetupStatus>("/auth/setup-status"))!.SetupAvailable;

    private sealed record SetupStatus(bool SetupAvailable);

    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public async Task MissingOrShortSecret_DisablesInstallation(string? secret)
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory(setupSecret: secret);
        using var client = factory.NewClient();
        Assert.False(await Available(client));
        Assert.Equal(HttpStatusCode.BadRequest, (await Setup(client)).StatusCode);
    }

    [Fact]
    public async Task Production_RequiresHttpsAndUsesSecureCookie()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory(environmentName: "Production");
        using var http = factory.NewClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await Setup(http)).StatusCode);
        using var https = factory.NewClient(https: true);
        var setup = await Setup(https);
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        Assert.Contains("secure", setup.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant());
        https.DefaultRequestHeaders.Add("Cookie", Cookie(setup));
        Assert.Equal(HttpStatusCode.OK, (await https.GetAsync("/auth/current")).StatusCode);
        http.DefaultRequestHeaders.Add("Cookie", Cookie(setup));
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/auth/current")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Login(http)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/auth/logout", new { })).StatusCode);
    }

    [Fact]
    public async Task Setup_ValidatesSecretPasswordAndCsrf_WithoutChangingInstallation()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var client = factory.NewClient();
        Assert.True(await Available(client));
        Assert.Equal(HttpStatusCode.BadRequest, (await Setup(client, secret: "wrong")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Setup(client, password: "weak")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Session-Protection");
        Assert.Equal(HttpStatusCode.Forbidden, (await Setup(client)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Session-Protection", "1");
        client.DefaultRequestHeaders.Remove("Origin");
        client.DefaultRequestHeaders.Add("Origin", "https://attacker.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await Setup(client)).StatusCode);
        await factory.WithDatabase(async db =>
        {
            Assert.False(await db.Installations.AnyAsync());
            Assert.False(await db.LocalAdministrators.AnyAsync());
            Assert.False(await db.AdministratorSessions.AnyAsync());
        });
    }

    [Fact]
    public async Task Setup_CreatesExplicitAdministratorAndHashedBoundedSession_NotSeedOwner()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var client = factory.NewClient();
        var response = await Setup(client);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("httponly", response.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant());
        Assert.Contains("samesite=strict", response.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant());
        var profile = (await response.Content.ReadFromJsonAsync<LocalAuthenticationEndpoints.AdministratorProfile>())!;
        Assert.Equal(LocalSessionAuthenticationHandler.AdministratorRole, profile.Role);
        Assert.NotEqual(LocalAuthenticationWebApplicationFactory.SeedOwnerId, profile.Id);
        client.DefaultRequestHeaders.Add("Cookie", Cookie(response));
        Assert.Equal(profile, await client.GetFromJsonAsync<LocalAuthenticationEndpoints.AdministratorProfile>("/auth/current"));
        Assert.False(await Available(client));
        await factory.WithDatabase(async db =>
        {
            var admin = await db.LocalAdministrators.SingleAsync();
            Assert.NotEqual(Password, admin.PasswordHash);
            var session = await db.AdministratorSessions.SingleAsync();
            Assert.NotEqual(Cookie(response).Split('=')[1], session.TokenHash);
            Assert.Equal(64, session.TokenHash.Length);
            Assert.InRange(session.ExpiresAt, DateTimeOffset.UtcNow.AddHours(7), DateTimeOffset.UtcNow.AddHours(8));
            Assert.Equal(2, await db.Users.CountAsync());
        });
        Assert.Equal(HttpStatusCode.Conflict, (await Setup(client, email: "second@test.example")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentSetup_CreatesOnlyOneAdministratorAndMarker()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var first = factory.NewClient();
        using var second = factory.NewClient();
        var responses = await Task.WhenAll(Setup(first), Setup(second, "second@test.example"));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        await factory.WithDatabase(async db =>
        {
            Assert.Equal(1, await db.Installations.CountAsync());
            Assert.Equal(1, await db.LocalAdministrators.CountAsync());
            Assert.Equal(1, await db.AdministratorSessions.CountAsync());
        });
    }

    [Fact]
    public async Task RemovingAllUsers_DoesNotReopenInstallation()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var client = factory.NewClient();
        Assert.Equal(HttpStatusCode.OK, (await Setup(client)).StatusCode);
        await factory.WithDatabase(async db =>
        {
            await db.Teams.ExecuteDeleteAsync();
            await db.Users.ExecuteDeleteAsync();
            Assert.Equal(0, await db.Users.CountAsync());
        });
        Assert.False(await Available(client));
        Assert.Equal(HttpStatusCode.Conflict, (await Setup(client)).StatusCode);
    }

    [Fact]
    public async Task Login_LocksOutInvalidCredentials_ThenAllowsLoginAfterLockoutExpires()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var client = factory.NewClient();
        Assert.Equal(HttpStatusCode.OK, (await Setup(client)).StatusCode);
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "incorrect")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client)).StatusCode);
        await factory.WithDatabase(async db =>
        {
            var admin = await db.LocalAdministrators.SingleAsync();
            Assert.True(admin.LockoutUntil > DateTimeOffset.UtcNow);
            admin.LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        });
        var response = await Login(client);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", Cookie(response));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/auth/current")).StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesCookie_AndUnsafeSessionRequestsRequireCsrf()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var client = factory.NewClient();
        var response = await Setup(client);
        client.DefaultRequestHeaders.Add("Cookie", Cookie(response));
        client.DefaultRequestHeaders.Remove("X-Session-Protection");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/teams", new { name = "Blocked" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/auth/logout", new { })).StatusCode);
        client.DefaultRequestHeaders.Add("X-Session-Protection", "1");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/current")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/teams")).StatusCode);
    }

    [Fact]
    public async Task Administrator_CannotAccessSeedOwnersTeam_AndBearerIsStillSupported()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var client = factory.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/teams")).StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", Cookie(await Setup(client)));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/teams/{LocalAuthenticationWebApplicationFactory.SeedTeamId}")).StatusCode);
        Assert.Equal("[]", await client.GetStringAsync("/teams"));

        var token = new JwtSecurityToken("LocalAuthTests", "LocalAuthTests",
            [new Claim(ClaimTypes.NameIdentifier, LocalAuthenticationWebApplicationFactory.SeedOwnerId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(LocalAuthenticationWebApplicationFactory.JwtKey)),
                SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/teams/{LocalAuthenticationWebApplicationFactory.SeedTeamId}")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/teams")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/current")).StatusCode);

        using var bearerOnly = factory.NewClient();
        bearerOnly.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.Forbidden, (await bearerOnly.GetAsync("/auth/current")).StatusCode);
    }

    [Fact]
    public async Task InactiveAccountAndExpiredSession_AreRejected()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var client = factory.NewClient();
        client.DefaultRequestHeaders.Add("Cookie", Cookie(await Setup(client)));
        await factory.WithDatabase(async db =>
        {
            var session = await db.AdministratorSessions.SingleAsync();
            session.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/current")).StatusCode);
        var login = await Login(client);
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", Cookie(login));
        await factory.WithDatabase(async db =>
        {
            var admin = await db.LocalAdministrators.SingleAsync();
            admin.IsActive = false;
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/current")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client)).StatusCode);
    }

    [Fact]
    public async Task Login_IsRateLimited()
    {
        await using var factory = new LocalAuthenticationWebApplicationFactory();
        using var client = factory.NewClient();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login(client)).StatusCode);
    }
}
