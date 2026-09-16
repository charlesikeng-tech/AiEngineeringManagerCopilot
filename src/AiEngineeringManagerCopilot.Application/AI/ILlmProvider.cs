namespace AiEngineeringManagerCopilot.Application.AI;

public interface ILlmProvider
{
    Task<LlmAnalysisResult> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken);
}