using System.Text.RegularExpressions;

namespace AiEngineeringManagerCopilot.Application.MicrosoftTeams;

public static partial class MicrosoftTeamsWebhookAddress
{
    public static bool TryParse(string? value, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var candidate) ||
            candidate.Scheme != Uri.UriSchemeHttps ||
            candidate.Port != 443 ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            !string.IsNullOrEmpty(candidate.Fragment))
        {
            return false;
        }

        var host = candidate.IdnHost;
        var isWorkflow =
            (host.EndsWith(".environment.api.powerplatform.com", StringComparison.OrdinalIgnoreCase) &&
             PowerPlatformPath().IsMatch(candidate.AbsolutePath)) ||
            (host.EndsWith(".logic.azure.com", StringComparison.OrdinalIgnoreCase) &&
             LogicAppPath().IsMatch(candidate.AbsolutePath));
        var isIncomingWebhook =
            (string.Equals(host, "outlook.office.com", StringComparison.OrdinalIgnoreCase) ||
             host.EndsWith(".webhook.office.com", StringComparison.OrdinalIgnoreCase)) &&
            IncomingWebhookPath().IsMatch(candidate.AbsolutePath);

        if (!isWorkflow && !isIncomingWebhook)
        {
            return false;
        }

        if (isWorkflow && !candidate.Query.TrimStart('?').Split('&')
                .Any(parameter => parameter.StartsWith("sig=", StringComparison.Ordinal) &&
                                  parameter.Length > 4))
        {
            return false;
        }

        uri = candidate;
        return true;
    }

    [GeneratedRegex(@"^/powerautomate/automations/direct/workflows/[^/]+/triggers/manual/paths/invoke$")]
    private static partial Regex PowerPlatformPath();

    [GeneratedRegex(@"^/workflows/[^/]+/triggers/manual/paths/invoke$")]
    private static partial Regex LogicAppPath();

    [GeneratedRegex(@"^/webhook(?:b2)?/[^/]+/.+")]
    private static partial Regex IncomingWebhookPath();
}
