using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class EngineeringRisk
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public Guid ReportId { get; set; }

    public RiskSeverity Severity { get; set; }

    public RiskCategory Category { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}