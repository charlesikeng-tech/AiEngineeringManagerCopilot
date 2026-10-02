using AiEngineeringManagerCopilot.Application.Common;

namespace AiEngineeringManagerCopilot.Application.Jira;

public static class JiraDestination
{
    public static Uri Validate(string? baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !uri.IdnHost.EndsWith(".atlassian.net", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["BaseUrl"] = ["Use an HTTPS Jira Cloud tenant URL (https://tenant.atlassian.net)."]
            });
        }

        return uri;
    }
}
