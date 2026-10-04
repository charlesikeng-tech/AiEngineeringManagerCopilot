using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AiEngineeringManagerCopilot.Api.Authentication;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class LocalAuthenticationEndpoints
{
    public sealed record SetupRequest(string Secret, string Email, string Name, string Password);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record AdministratorProfile(Guid Id, string? Email, string Name, string Role, bool EmailVerified = false);

    public static void MapLocalAuthenticationEndpoints(this WebApplication app)
    {
        app.MapSsoEndpoints();
        app.MapSsoLoginEndpoints();
        var group = app.MapGroup("/auth").WithTags("Local administrator");
        group.MapGet("/setup-status", async (AppDbContext db, IConfiguration config, CancellationToken ct) =>
            Results.Ok(new { setupAvailable = SecretConfigured(config) && !await db.Installations.AnyAsync(ct) }))
            .AllowAnonymous();
        group.MapPost("/setup", SetupAsync).AllowAnonymous().RequireRateLimiting("LocalAuthentication");
        group.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting("LocalAuthentication");
        group.MapGet("/current", async (HttpContext context, AppDbContext db, CancellationToken ct) =>
        {
            var id = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            return user is null ? Results.Unauthorized() : Results.Ok(new AdministratorProfile(
                user.Id, user.Email, user.Name, context.User.FindFirstValue(ClaimTypes.Role)!, user.EmailVerified));
        }).RequireAuthorization("Session");
        group.MapPost("/logout", async (HttpContext context, AppDbContext db, IWebHostEnvironment env, CancellationToken ct) =>
        {
            await RevokePresentedSessionAsync(context, db, ct);
            context.Response.Cookies.Delete(LocalSessionAuthenticationHandler.CookieName, CookieOptions(env));
            return Results.NoContent();
        }).RequireAuthorization("Session");
    }

    private static bool SecretConfigured(IConfiguration config)
    {
        var secret = config["Authentication:SetupSecret"];
        return secret is { Length: >= 43 and <= 512 };
    }

    private static async Task<IResult> SetupAsync(
        SetupRequest request, HttpContext context, AppDbContext db, IConfiguration config,
        UserManager<IdentityUser> manager, IPasswordHasher<IdentityUser> hasher,
        IWebHostEnvironment env, CancellationToken ct)
    {
        if (!SecretConfigured(config) || request.Secret is null || request.Secret.Length > 512 ||
            !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(config["Authentication:SetupSecret"]!)),
                SHA256.HashData(Encoding.UTF8.GetBytes(request.Secret))))
            return Results.BadRequest(new { error = "Installation unavailable or invalid secret." });

        if (!ValidEmail(request.Email) || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200 ||
            request.Password is null || request.Password.Length > 128)
            return Results.BadRequest(new { error = "Invalid administrator details." });

        var identityUser = new IdentityUser { UserName = request.Email.Trim(), Email = request.Email.Trim() };
        foreach (var validator in manager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(manager, identityUser, request.Password);
            if (!result.Succeeded)
                return Results.BadRequest(new { error = "Password must contain at least 12 characters, uppercase, lowercase, a digit and a symbol." });
        }

        var passwordHash = hasher.HashPassword(identityUser, request.Password);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // A transaction-scoped PostgreSQL lock serializes installation across processes.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7192836401)", ct);
        if (await db.Installations.AnyAsync(ct))
            return Results.Conflict(new { error = "Installation is already complete." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email != null && x.Email.ToLower() == email, ct))
            return Results.BadRequest(new { error = "This email cannot be used for installation." });

        var user = new User { Id = Guid.NewGuid(), Email = email, Name = request.Name.Trim(), CreatedAt = DateTimeOffset.UtcNow };
        db.Users.Add(user);
        db.LocalAdministrators.Add(new LocalAdministrator
        {
            UserId = user.Id, NormalizedEmail = email.ToUpperInvariant(), PasswordHash = passwordHash,
            IsActive = true, Role = LocalSessionAuthenticationHandler.AdministratorRole
        });
        db.Installations.Add(new Installation { InitializedAt = DateTimeOffset.UtcNow });
        await RevokePresentedSessionAsync(context, db, ct);
        var token = AddSession(db, user.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        WriteCookie(context, env, token);
        return Results.Ok(Profile(user));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, HttpContext context, AppDbContext db,
        IPasswordHasher<IdentityUser> hasher, IWebHostEnvironment env, CancellationToken ct)
    {
        if (!ValidEmail(request.Email) || request.Password is null || request.Password.Length > 128)
            return Results.Unauthorized();

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var admin = await db.LocalAdministrators
            .FromSqlInterpolated($"SELECT * FROM \"LocalAdministrators\" WHERE \"NormalizedEmail\" = {normalizedEmail} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (admin is null || !admin.IsActive || admin.Role != LocalSessionAuthenticationHandler.AdministratorRole ||
            admin.LockoutUntil > DateTimeOffset.UtcNow)
        {
            // Perform a real Identity hash verification even for missing or locked accounts.
            hasher.VerifyHashedPassword(new IdentityUser(), DummyPasswordHash, request.Password);
            return Results.Unauthorized();
        }
        var identityUser = new IdentityUser { UserName = request.Email };
        var result = hasher.VerifyHashedPassword(identityUser, admin.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            admin.FailedLoginCount++;
            if (admin.FailedLoginCount >= 5)
            {
                admin.LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(15);
                admin.FailedLoginCount = 0;
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Unauthorized();
        }

        var user = await db.Users.SingleAsync(x => x.Id == admin.UserId, ct);
        if (!user.IsActive) return Results.Unauthorized();
        admin.FailedLoginCount = 0;
        admin.LockoutUntil = null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            admin.PasswordHash = hasher.HashPassword(identityUser, request.Password);
        await db.AdministratorSessions.Where(x => x.ExpiresAt <= DateTimeOffset.UtcNow).ExecuteDeleteAsync(ct);
        await RevokePresentedSessionAsync(context, db, ct);
        var token = AddSession(db, user.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        WriteCookie(context, env, token);
        return Results.Ok(Profile(user));
    }

    private static readonly string DummyPasswordHash =
        new PasswordHasher<IdentityUser>().HashPassword(new IdentityUser(), Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    private static bool ValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && email.Trim().Length <= 254 &&
        MailAddress.TryCreate(email.Trim(), out var address) && address.Address == email.Trim();

    private static AdministratorProfile Profile(User user) =>
        new(user.Id, user.Email, user.Name, LocalSessionAuthenticationHandler.AdministratorRole);

    private static string AddSession(AppDbContext db, Guid userId)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.AdministratorSessions.Add(new AdministratorSession
        {
            TokenHash = LocalSessionAuthenticationHandler.Hash(token), UserId = userId,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(8)
        });
        return token;
    }

    private static CookieOptions CookieOptions(IWebHostEnvironment env) => new()
    {
        HttpOnly = true, Secure = !env.IsDevelopment() && !env.IsEnvironment("Test"),
        SameSite = SameSiteMode.Strict, Path = "/", MaxAge = TimeSpan.FromHours(8), IsEssential = true
    };

    internal static async Task RevokePresentedSessionAsync(HttpContext context, AppDbContext db, CancellationToken ct)
    {
        if (!context.Request.Cookies.TryGetValue(LocalSessionAuthenticationHandler.CookieName, out var token)) return;
        var hash = LocalSessionAuthenticationHandler.Hash(token);
        await db.AdministratorSessions.Where(x => x.TokenHash == hash).ExecuteDeleteAsync(ct);
        await db.SsoSessions.Where(x => x.TokenHash == hash).ExecuteDeleteAsync(ct);
    }

    internal static void WriteCookie(HttpContext context, IWebHostEnvironment env, string token) =>
        context.Response.Cookies.Append(LocalSessionAuthenticationHandler.CookieName, token, CookieOptions(env));
}
