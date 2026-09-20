namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class JiraWorkItem
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? AssigneeExternalId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DoneAt { get; set; }

    public bool IsBlocked { get; set; }
}