namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class LocalAdministrator
{
    public Guid UserId { get; set; }
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Role { get; set; } = "PlatformAdministrator";
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }
}
