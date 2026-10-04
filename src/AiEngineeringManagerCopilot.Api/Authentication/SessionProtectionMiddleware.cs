namespace AiEngineeringManagerCopilot.Api.Authentication;

public sealed class SessionProtectionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var request = context.Request;
        var authPath = request.Path.StartsWithSegments("/auth");
        if (authPath) context.Response.Headers.CacheControl = "no-store";
        var mutation = !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) &&
                       !HttpMethods.IsOptions(request.Method);
        var sessionRequest = context.User.Identity?.AuthenticationType is
            LocalSessionAuthenticationHandler.Scheme or SsoSessionAuthenticationHandler.Scheme;
        if (mutation && (authPath || sessionRequest))
        {
            var origin = request.Headers.Origin.ToString();
            var sameOrigin = $"{request.Scheme}://{request.Host}";
            var allowed = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (request.Headers["X-Session-Protection"] != "1" ||
                (origin != sameOrigin && !allowed.Contains(origin, StringComparer.OrdinalIgnoreCase)) ||
                (!request.IsHttps && !environment.IsDevelopment() && !environment.IsEnvironment("Test")))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }
        await next(context);
    }
}
