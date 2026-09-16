namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class Repository
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public long ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? DefaultBranch { get; set; }

    public bool IsActive { get; set; }
}