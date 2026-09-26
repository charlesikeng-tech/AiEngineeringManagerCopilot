namespace AiEngineeringManagerCopilot.Application.AI;

public sealed record AIAnalysisResult(
    string Summary,
    IReadOnlyList<LlmInsightResult> Insights,
    IReadOnlyList<LlmActionResult> Actions,
    IReadOnlyList<LlmEvidenceResult>? Evidence = null)
{
    public IReadOnlyList<LlmEvidenceResult> SafeEvidence =>
        Evidence ?? Array.Empty<LlmEvidenceResult>();
}