namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class EngineeringReport
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public string ExecutiveSummary { get; set; } = string.Empty;

    public int OverallScore { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}