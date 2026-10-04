namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed class ReportNotificationFactory(ReportNotificationOptions options)
{
    public ReportNotification Create(Guid teamId, string teamName, EngineeringReportResponse report)
    {
        if (report.TeamId != teamId)
        {
            throw new ArgumentException("The report does not belong to the notification team.", nameof(report));
        }
        if (!ReportNotificationOptions.TryParseBaseUrl(options.FrontendBaseUrl, out var baseUrl))
        {
            throw new InvalidOperationException("Report notification frontend base URL is invalid.");
        }

        var reportUrl = new Uri(
            $"{baseUrl!.AbsoluteUri.TrimEnd('/')}/reports/{report.Id:D}?teamId={teamId:D}");
        var risks = report.Risks.OrderByDescending(risk => risk.Severity)
            .ThenByDescending(risk => risk.CreatedAt).ThenBy(risk => risk.Id)
            .Take(3).Select(risk => new ReportNotificationItem(
                risk.Severity.ToString(), Limit(risk.Title, 80), Limit(risk.Description, 160))).ToArray();
        var actions = report.Actions.Where(action => action.Status is not ("Done" or "Cancelled"))
            .OrderByDescending(action => action.Priority)
            .ThenByDescending(action => action.CreatedAt).ThenBy(action => action.Id)
            .Take(3).Select(action => new ReportNotificationItem(
                action.Priority.ToString(), Limit(action.Title, 80), Limit(action.Description, 160))).ToArray();

        return new ReportNotification(
            Limit(teamName, 120), report.PeriodStart, report.PeriodEnd, report.OverallScore,
            Limit(report.HealthLevel, 60), report.DataCoverage, Limit(report.ExecutiveSummary, 400),
            report.Risks.Count, report.Actions.Count, risks, actions, reportUrl);
    }

    private static string Limit(string value, int length)
    {
        if (value.Length <= length) return value;
        var end = length - 3;
        if (char.IsHighSurrogate(value[end - 1])) end--;
        return value[..end] + "...";
    }
}
