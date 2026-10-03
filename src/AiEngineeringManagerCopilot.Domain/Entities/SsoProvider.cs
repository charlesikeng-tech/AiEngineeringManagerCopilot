namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class SsoProvider
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string? ProtectedSecret { get; set; }
    public int Revision { get; set; } = 1;
    public int? TestedRevision { get; set; }
    public DateTimeOffset? TestedAt { get; set; }
    public string? LastTestError { get; set; }
    public Guid? CurrentTestId { get; set; }
    public string? ActiveConfiguration { get; set; }
    public int? ActiveRevision { get; set; }
}

public sealed class SsoConnectionTest
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public int Revision { get; set; }
    public string SessionHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Consumed { get; set; }
}
