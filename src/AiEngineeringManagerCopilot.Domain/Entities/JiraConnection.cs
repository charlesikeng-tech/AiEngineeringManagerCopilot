namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class JiraConnection
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string ApiTokenEncrypted { get; set; } = string.Empty;
    
    public string ProjectKey { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastSyncAt { get; set; }
}