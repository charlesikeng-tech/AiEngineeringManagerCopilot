using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Api.Authentication;

public static class AdministratorSessionProof
{
    public static IQueryable<string> ValidHashes(AppDbContext db) =>
        (from s in db.AdministratorSessions
         join a in db.LocalAdministrators on s.UserId equals a.UserId
         join u in db.Users on s.UserId equals u.Id
         where s.ExpiresAt > DateTimeOffset.UtcNow && a.IsActive && u.IsActive &&
             a.Role == LocalSessionAuthenticationHandler.AdministratorRole
         select s.TokenHash).Concat(
            from s in db.SsoSessions
            join i in db.ExternalIdentities on s.ExternalIdentityId equals i.Id
            join a in db.LocalAdministrators on s.UserId equals a.UserId
            join u in db.Users on s.UserId equals u.Id
            join p in db.SsoProviders on s.ProviderId equals p.Id
            where s.ExpiresAt > DateTimeOffset.UtcNow && i.UserId == u.Id && i.AdministratorAccessApproved &&
                a.IsActive && u.IsActive && a.Role == LocalSessionAuthenticationHandler.AdministratorRole &&
                p.ActiveRevision == s.ActiveRevision && p.ActiveConfiguration == s.ActiveConfiguration
            select s.TokenHash);

    // Lock all source proof rows until the linking transaction commits. The Strict
    // cookie is deliberately not required on a cross-site OIDC callback.
    public static Task<AdministratorSession?> LockLocalAsync(AppDbContext db, string hash, Guid userId, CancellationToken ct) =>
        db.AdministratorSessions.FromSqlInterpolated($"""
            SELECT s.* FROM "AdministratorSessions" s
            JOIN "LocalAdministrators" a ON a."UserId" = s."UserId"
            JOIN users u ON u."Id" = s."UserId"
            WHERE s."TokenHash" = {hash} AND s."UserId" = {userId}
              AND s."ExpiresAt" > {DateTimeOffset.UtcNow}
              AND a."IsActive" AND u."IsActive" AND a."Role" = {LocalSessionAuthenticationHandler.AdministratorRole}
            FOR UPDATE OF s, a, u
            """).SingleOrDefaultAsync(ct);
}
