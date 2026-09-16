namespace AiEngineeringManagerCopilot.Application.AI;

public interface IAIAnalysisService
{
    Task<AIAnalysisResult> AnalyzeAsync(
        Guid teamId,
        Guid reportId,
        CancellationToken cancellationToken);
}