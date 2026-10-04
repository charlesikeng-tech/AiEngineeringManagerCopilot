using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AiEngineeringManagerCopilot.Api.Authentication.Sso;

public sealed class SsoOidcHandlerFactory(
    IOptionsFactory<OpenIdConnectOptions> optionsFactory, ISecretProtector secrets, IWebHostEnvironment environment,
    SsoLoginDiagnostics diagnostics)
{
    public const string AttemptKey = "sso.attempt";

    public OpenIdConnectOptions Create(SsoProvider snapshot, Guid attemptId, string scheme,
        string callback, string callbackPath, HttpClient backchannel)
    {
        diagnostics.Stage = SsoLoginStage.Options;
        var options = optionsFactory.Create(scheme);
        options.CallbackPath = callbackPath;
        options.Authority = snapshot.Authority;
        options.ClientId = snapshot.ClientId;
        // Use the supported code + PKCE flow; the framework still rejects providers requiring PAR.
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
        diagnostics.Stage = SsoLoginStage.SecretDecryption;
        options.ClientSecret = snapshot.ProtectedSecret is null ? null : secrets.Unprotect(snapshot.ProtectedSecret);
        diagnostics.Stage = SsoLoginStage.Options;
        options.Backchannel = backchannel;
        options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            snapshot.Authority.TrimEnd('/') + "/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(backchannel) { RequireHttps = true });
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = snapshot.Authority,
            ValidateAudience = true, ValidAudience = snapshot.ClientId,
            ValidateIssuerSigningKey = true, RequireSignedTokens = true,
            ValidateLifetime = true, RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events.OnAuthorizationCodeReceived = context =>
        {
            context.TokenEndpointRequest!.RedirectUri = callback;
            return Task.CompletedTask;
        };
        options.Events.OnTokenValidated = context =>
        {
            if (context.SecurityToken.Issuer != snapshot.Authority)
                context.Fail("Issuer does not match the configured tenant.");
            return Task.CompletedTask;
        };
        options.Events.OnMessageReceived = context =>
        {
            if (context.Properties is null ||
                !context.Properties.Items.TryGetValue(AttemptKey, out var id) || id != attemptId.ToString("D"))
                context.Fail("Invalid authentication state.");
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            context.ProtocolMessage.RedirectUri = callback;
            var target = new Uri(context.ProtocolMessage.IssuerAddress);
            if (target.Scheme != "https" || target.Host != new Uri(snapshot.Authority).Host ||
                target.Port != 443 || target.UserInfo != "")
                throw new InvalidOperationException("Untrusted authorization endpoint.");
            return Task.CompletedTask;
        };
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.Path = new Uri(callback).AbsolutePath;
        options.CorrelationCookie.Path = new Uri(callback).AbsolutePath;
        var secure = environment.IsDevelopment() || environment.IsEnvironment("Test")
            ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.NonceCookie.SecurePolicy = secure;
        options.CorrelationCookie.SecurePolicy = secure;
        return options;
    }

    public static async Task<OpenIdConnectHandler> HandlerAsync(HttpContext context, OpenIdConnectOptions options, string scheme)
    {
        var handler = ActivatorUtilities.CreateInstance<OpenIdConnectHandler>(
            context.RequestServices, new FixedOptions(options), NullLoggerFactory.Instance);
        await handler.InitializeAsync(new AuthenticationScheme(scheme, scheme, typeof(OpenIdConnectHandler)), context);
        return handler;
    }

    private sealed class FixedOptions(OpenIdConnectOptions options) : IOptionsMonitor<OpenIdConnectOptions>
    {
        public OpenIdConnectOptions CurrentValue => options;
        public OpenIdConnectOptions Get(string? name) => options;
        public IDisposable? OnChange(Action<OpenIdConnectOptions, string?> listener) => null;
    }
}
