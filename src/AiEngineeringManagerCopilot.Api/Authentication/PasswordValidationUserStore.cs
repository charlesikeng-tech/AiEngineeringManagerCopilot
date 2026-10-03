using Microsoft.AspNetCore.Identity;

namespace AiEngineeringManagerCopilot.Api.Authentication;

// Identity Core is used only for password validation and hashing, not account persistence.
public sealed class PasswordValidationUserStore : IUserStore<IdentityUser>
{
    public void Dispose() { }
    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
    public Task<string?> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
    public Task SetUserNameAsync(IdentityUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }
    public Task<string?> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
    public Task SetNormalizedUserNameAsync(IdentityUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }
    public Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<IdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotSupportedException();
}
