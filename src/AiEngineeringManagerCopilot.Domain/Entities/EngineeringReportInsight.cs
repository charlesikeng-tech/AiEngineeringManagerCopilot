using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class EngineeringReportInsight
{
    public Guid Id { get; set; }

    public Guid ReportId { get; set; }

    public MetricType? MetricType { get; set; }

    public RiskCategory Category { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Impact { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;
}