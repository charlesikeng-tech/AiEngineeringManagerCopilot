using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class EngineeringAction
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ActionPriority Priority { get; set; }

    public string? Owner { get; set; }
    public DateOnly? DueDate { get; set; }
    public ActionStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}