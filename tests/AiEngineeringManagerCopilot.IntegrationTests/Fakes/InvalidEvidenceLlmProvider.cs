using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.IntegrationTests.Fakes;

public sealed class InvalidEvidenceLlmProvider : ILlmProvider
{
    public Task<LlmAnalysisResult> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        var result = new LlmAnalysisResult(
            Summary: "Invalid evidence test.",
            Insights: [],
            Actions: [],
            Evidence:
            [
                new LlmEvidenceResult(
                    MetricType: "CycleTime",
                    Value: 999m,
                    Reason: "Invalid evidence for testing.",
                    Confidence: 0.92m)
            ]);

        return Task.FromResult(result);
    }
}