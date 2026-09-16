namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class EngineeringReportInsight
{
    public Guid Id { get; set; }

    public Guid ReportId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Impact { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;
}