using System.Security.Cryptography;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Api.Authentication;

public static class LocalPasswordConfirmation
{
    private static readonly string DummyHash = new PasswordHasher<IdentityUser>()
        .HashPassword(new IdentityUser(), Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    // Caller owns the transaction and locks the credential row. Failure counters must
    // commit even when confirmation fails; no sessions/identities are changed here.
    public static async Task<User?> VerifyAsync(LocalAdministrator? admin, string? password,
        AppDbContext db, IPasswordHasher<IdentityUser> hasher, CancellationToken ct)
    {
        if (password is null || password.Length > 128) return null;
        if (admin is null || !admin.IsActive || admin.Role != LocalSessionAuthenticationHandler.AdministratorRole ||
            admin.LockoutUntil > DateTimeOffset.UtcNow)
        {
            hasher.VerifyHashedPassword(new IdentityUser(), DummyHash, password);
            return null;
        }
        var identityUser = new IdentityUser { UserName = admin.NormalizedEmail };
        var result = hasher.VerifyHashedPassword(identityUser, admin.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            admin.FailedLoginCount++;
            if (admin.FailedLoginCount >= 5)
            {
                admin.LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(15);
                admin.FailedLoginCount = 0;
            }
            await db.SaveChangesAsync(ct);
            return null;
        }
        var user = await db.Users.SingleAsync(x => x.Id == admin.UserId, ct);
        if (!user.IsActive) return null;
        admin.FailedLoginCount = 0;
        admin.LockoutUntil = null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            admin.PasswordHash = hasher.HashPassword(identityUser, password);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
