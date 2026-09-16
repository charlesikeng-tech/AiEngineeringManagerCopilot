namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class GitHubConnection
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public string Organization { get; set; } = string.Empty;

    public string AccessTokenEncrypted { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastSyncAt { get; set; }
}