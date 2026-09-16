using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.AI;

public sealed record LlmAnalysisResult(
    string Summary,
    IReadOnlyList<LlmInsightResult> Insights,
    IReadOnlyList<LlmActionResult> Actions);
    
    
public sealed record LlmInsightResult(
    string Category,
    string Title,
    string Description,
    string Impact,
    string Recommendation);

public sealed record LlmActionResult(
    string Title,
    string Description,
    ActionPriority Priority);