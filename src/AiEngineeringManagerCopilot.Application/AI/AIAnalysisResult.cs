namespace AiEngineeringManagerCopilot.Application.AI;

public sealed record AIAnalysisResult(
    string Summary,
    IReadOnlyList<LlmInsightResult> Insights,
    IReadOnlyList<LlmActionResult> Actions);