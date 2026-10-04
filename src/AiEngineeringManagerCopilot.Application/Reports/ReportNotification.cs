using System.Globalization;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record ReportNotificationItem(string Priority, string Title, string Description);

public sealed record ReportNotification(
    string TeamName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int OverallScore,
    string HealthLevel,
    decimal DataCoverage,
    string ExecutiveSummary,
    int RiskCount,
    int ActionCount,
    IReadOnlyList<ReportNotificationItem> Risks,
    IReadOnlyList<ReportNotificationItem> Actions,
    Uri ReportUrl)
{
    public string Period => $"{PeriodStart:yyyy-MM-dd} - {PeriodEnd:yyyy-MM-dd}";
    public string Coverage => $"{DataCoverage.ToString("0.#", CultureInfo.InvariantCulture)}%";
    public string FallbackText =>
        $"Engineering report generated\nTeam: {TeamName}\nPeriod: {Period}\n" +
        $"Health score: {OverallScore}/100 ({HealthLevel})\nData coverage: {Coverage}\n" +
        $"{ExecutiveSummary}\nTop risks:\n{ItemSummary(Risks, "No risks recorded.")}\n" +
        $"Priority actions:\n{ItemSummary(Actions, "No open recommended actions recorded.")}\n" +
        $"View report: {ReportUrl.AbsoluteUri}";

    private static string ItemSummary(IReadOnlyList<ReportNotificationItem> items, string emptyMessage) =>
        items.Count == 0 ? emptyMessage : string.Join("\n", items.Select(item => $"- [{item.Priority}] {item.Title}"));
}
