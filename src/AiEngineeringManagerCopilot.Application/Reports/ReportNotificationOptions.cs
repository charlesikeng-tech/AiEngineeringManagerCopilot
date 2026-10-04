namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed class ReportNotificationOptions
{
    public const string SectionName = "ReportNotifications";

    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

    public static bool TryParseBaseUrl(string? value, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1024 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var candidate) ||
            candidate.AbsoluteUri.Length > 1024 ||
            (candidate.Scheme != Uri.UriSchemeHttps && candidate.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            !string.IsNullOrEmpty(candidate.Query) ||
            !string.IsNullOrEmpty(candidate.Fragment))
        {
            return false;
        }
        uri = candidate;
        return true;
    }
}
