namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class ExternalIdentity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Issuer { get; set; } = "";
    public string Subject { get; set; } = "";
}

public sealed class SsoSession
{
    public string TokenHash { get; set; } = "";
    public Guid UserId { get; set; }
    public Guid ExternalIdentityId { get; set; }
    public Guid ProviderId { get; set; }
    public int ActiveRevision { get; set; }
    public string ActiveConfiguration { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class SsoLoginAttempt
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public int ActiveRevision { get; set; }
    public string ActiveConfiguration { get; set; } = "";
    public string? PreviousSessionHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Consumed { get; set; }
}
