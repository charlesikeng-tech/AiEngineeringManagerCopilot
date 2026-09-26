using System.Globalization;
using System.Text.RegularExpressions;
using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public sealed class FakeLlmProvider : ILlmProvider
{
    public string? LastPrompt { get; private set; }

    public Task<LlmAnalysisResult> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        LastPrompt = prompt;

        var cycleTime = ExtractMetricValue(
            prompt,
            "CycleTime");

        var result = new LlmAnalysisResult(
            Summary:
                "The engineering team shows several areas requiring attention.",
            Insights:
            [
                new LlmInsightResult(
                    Category: "Delivery",
                    Title: "Delivery performance needs attention",
                    Description:
                        "The current engineering metrics indicate delivery inefficiencies.",
                    Impact:
                        "Delivery predictability may be affected.",
                    Recommendation:
                        "Review the main delivery bottlenecks with the engineering team.")
            ],
            Actions:
            [
                new LlmActionResult(
                    Title: "Review delivery bottlenecks",
                    Description:
                        "Identify and address the main causes of delivery delays.",
                    Priority: ActionPriority.High)
            ],
            Evidence:
            [
                new LlmEvidenceResult(
                    MetricType: "CycleTime",
                    Value: cycleTime,
                    Reason:
                        "Cycle time indicates a potential delivery slowdown.",
                    Confidence: 0.92m)
            ]);

        return Task.FromResult(result);
    }

    private static decimal ExtractMetricValue(
        string prompt,
        string metricType)
    {
        var match = Regex.Match(
            prompt,
            $@"^- {Regex.Escape(metricType)}:\s*(-?\d+(?:\.\d+)?)\s*$",
            RegexOptions.Multiline);

        if (!match.Success)
        {
            return 4.8m;
        }

        return decimal.Parse(
            match.Groups[1].Value,
            CultureInfo.InvariantCulture);
    }
}