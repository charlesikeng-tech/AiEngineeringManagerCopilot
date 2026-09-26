namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class AIAnalysis
{
    public Guid Id { get; set; }

    public Guid ReportId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    
    public ICollection<AIAnalysisEvidence> Evidence { get; set; } =
        new List<AIAnalysisEvidence>();
}