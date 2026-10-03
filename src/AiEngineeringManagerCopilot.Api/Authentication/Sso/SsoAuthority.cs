using System.Text.RegularExpressions;

namespace AiEngineeringManagerCopilot.Api.Authentication.Sso;

public static partial class SsoAuthority
{
    public static string Normalize(string type, string tenant, string? authorizationServer)
    {
        tenant = tenant.Trim().ToLowerInvariant();
        if (type == "EntraId")
        {
            if (!Guid.TryParseExact(tenant, "D", out var id) || id == Guid.Empty)
                throw new ArgumentException("Entra ID requires a specific tenant GUID.");
            return $"https://login.microsoftonline.com/{id:D}/v2.0";
        }
        if (!HostPattern().IsMatch(tenant) || tenant.Length > 253)
            throw new ArgumentException("Enter a standard provider hostname, not a URL or custom domain.");
        var suffixes = type switch
        {
            "Auth0" => new[] { ".auth0.com" },
            "Okta" => new[] { ".okta.com", ".oktapreview.com", ".okta-emea.com" },
            _ => throw new ArgumentException("Unsupported provider type.")
        };
        if (!suffixes.Any(s => tenant.EndsWith(s, StringComparison.Ordinal) && tenant.Length > s.Length))
            throw new ArgumentException("Only standard Auth0 or Okta tenant domains are supported.");
        if (tenant.Split('.').Any(label => label.StartsWith("xn--", StringComparison.Ordinal)))
            throw new ArgumentException("IDN tenant domains are not supported.");
        if (type == "Auth0") return $"https://{tenant}/";
        var server = string.IsNullOrWhiteSpace(authorizationServer) ? "default" : authorizationServer.Trim();
        if (!ServerPattern().IsMatch(server))
            throw new ArgumentException("Invalid Okta authorization server identifier.");
        return $"https://{tenant}/oauth2/{server}";
    }

    [GeneratedRegex(@"^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)+$")]
    private static partial Regex HostPattern();
    [GeneratedRegex(@"^[a-zA-Z0-9_-]{1,100}$")]
    private static partial Regex ServerPattern();
}
