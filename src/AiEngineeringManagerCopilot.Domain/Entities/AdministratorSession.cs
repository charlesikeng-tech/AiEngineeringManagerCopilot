namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class AdministratorSession
{
    public string TokenHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
