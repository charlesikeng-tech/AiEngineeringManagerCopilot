namespace AiEngineeringManagerCopilot.Api.Authentication.Sso;

public sealed class SsoDeployment(IConfiguration configuration, IWebHostEnvironment environment)
{
    public const string CallbackPath = "/auth/sso/callback";

    public (string Callback, string Frontend) GetUrls()
    {
        var api = Parse(configuration["Authentication:Sso:PublicApiBaseUrl"], true);
        var frontend = Parse(configuration["Authentication:Sso:FrontendOrigin"], false);
        return (api.TrimEnd('/') + CallbackPath, frontend.TrimEnd('/') + "/admin/authentication");
    }

    private string Parse(string? value, bool allowPath)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || (!allowPath && uri.AbsolutePath != "/") ||
            (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.Host == "localhost" &&
                (environment.IsDevelopment() || environment.IsEnvironment("Test")))))
            throw new InvalidOperationException("Configure trusted SSO public API base URL and frontend origin (HTTPS in production).");
        return uri.AbsoluteUri;
    }
}
