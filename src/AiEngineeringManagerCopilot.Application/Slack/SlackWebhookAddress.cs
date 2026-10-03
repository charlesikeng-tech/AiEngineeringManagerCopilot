namespace AiEngineeringManagerCopilot.Application.Slack;

public static class SlackWebhookAddress
{
    public static bool TryParse(string? value, out Uri? uri)
    {
        uri = null;

        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var candidate))
        {
            return false;
        }

        if (candidate.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(candidate.Host, "hooks.slack.com", StringComparison.OrdinalIgnoreCase) ||
            candidate.Port != 443 ||
            !candidate.AbsolutePath.StartsWith("/services/", StringComparison.Ordinal) ||
            candidate.AbsolutePath.Length <= "/services/".Length ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            !string.IsNullOrEmpty(candidate.Query) ||
            !string.IsNullOrEmpty(candidate.Fragment)
            )
        {
            return false;
        }

        uri = candidate;
        return true;
    }
}
