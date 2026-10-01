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

        var evidence = TryExtractAvailableMetric(prompt);

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
            Evidence: evidence is null
                ? []
                :
                [
                    new LlmEvidenceResult(
                        MetricType: evidence.Value.MetricType,
                        Value: evidence.Value.Value,
                        Reason: BuildEvidenceReason(
                            evidence.Value.MetricType),
                        Confidence: 0.92m)
                ]);

        return Task.FromResult(result);
    }

    private static (string MetricType, decimal Value)?
        TryExtractAvailableMetric(string prompt)
    {
        var availableSection =
            ExtractAvailableMetricsSection(prompt);

        if (string.IsNullOrWhiteSpace(availableSection))
        {
            return null;
        }

        var match = Regex.Match(
            availableSection,
            @"^-\s+(?<metric>[A-Za-z0-9_]+):\s*" +
            @"(?<value>-?\d+(?:\.\d+)?)",
            RegexOptions.Multiline);

        if (!match.Success)
        {
            return null;
        }

        if (!decimal.TryParse(
                match.Groups["value"].Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value))
        {
            return null;
        }

        return (
            match.Groups["metric"].Value,
            value);
    }

    private static string BuildEvidenceReason(
        string metricType)
    {
        return metricType switch
        {
            "CycleTime" =>
                "Cycle time indicates a potential delivery slowdown.",

            _ =>
                "The available engineering metric supports the analysis."
        };
    }

    private static string? ExtractAvailableMetricsSection(
        string prompt)
    {
        const string startMarker =
            "## Available metrics";

        const string endMarker =
            "## Unavailable metrics";

        var startIndex = prompt.IndexOf(
            startMarker,
            StringComparison.Ordinal);

        if (startIndex < 0)
        {
            return null;
        }

        startIndex += startMarker.Length;

        var endIndex = prompt.IndexOf(
            endMarker,
            startIndex,
            StringComparison.Ordinal);

        if (endIndex < 0)
        {
            return prompt[startIndex..];
        }

        return prompt[startIndex..endIndex];
    }
}