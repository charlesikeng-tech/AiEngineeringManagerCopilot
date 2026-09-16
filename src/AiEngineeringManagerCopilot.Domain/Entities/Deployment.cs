namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class Deployment
{
    public Guid Id { get; set; }

    public Guid RepositoryId { get; set; }

    public long ExternalId { get; set; }

    public string Environment { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset DeployedAt { get; set; }
}