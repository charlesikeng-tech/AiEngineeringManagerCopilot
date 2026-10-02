using System.Text.Json;
using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringReportSnapshot(
    int Version,
    IReadOnlyList<AIAnalysisMetricContext> MetricContext,
    IReadOnlyList<EngineeringReportMetricResponse> Metrics,
    IReadOnlyList<MetricTrendResult> Trends)
{
    public string Serialize() => JsonSerializer.Serialize(this);

    public static EngineeringReportSnapshot? Read(EngineeringReport report)
    {
        if (report.SnapshotJson is null) return null;
        var snapshot = JsonSerializer.Deserialize<EngineeringReportSnapshot>(report.SnapshotJson);
        if (snapshot is null || snapshot.Version != 1)
            throw new InvalidOperationException("Unsupported engineering report snapshot.");
        return snapshot;
    }
}
