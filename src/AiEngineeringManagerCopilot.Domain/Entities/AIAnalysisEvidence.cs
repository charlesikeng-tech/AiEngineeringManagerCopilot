namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class AIAnalysisEvidence
{
    public Guid Id { get; set; }

    public Guid AIAnalysisId { get; set; }

    public string MetricType { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public string Reason { get; set; } = string.Empty;

    public decimal Confidence { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    
    public void SetConfidence(decimal confidence)
    {
        if (confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence),
                "Confidence must be between 0 and 1.");
        }

        Confidence = confidence;
    }
}