using AiEngineeringManagerCopilot.Api.Authentication.Sso;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.Api.DependencyInjection;

public static class SsoServiceCollectionExtensions
{
    public static IServiceCollection AddSsoServices(this IServiceCollection services, IConfiguration configuration)
    {
        var keyRingPath = configuration["Authentication:DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(keyRingPath))
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        services.AddSingleton<SsoDeployment>();
        services.AddSingleton<SsoProtocolHttpClientFactory>();
        services.AddScoped<SsoConnectionFlow>();
        services.Configure<OpenIdConnectOptions>(SsoConnectionFlow.Scheme, options =>
        {
            options.SignInScheme = "LocalSession"; // TicketReceived always handles the response; never signs in.
            options.CallbackPath = SsoDeployment.CallbackPath;
            options.ResponseType = "code";
            options.ResponseMode = "query";
            options.UsePkce = true;
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = false;
            options.MapInboundClaims = false;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.RemoteAuthenticationTimeout = TimeSpan.FromMinutes(10);
        });
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IPostConfigureOptions<OpenIdConnectOptions>, OpenIdConnectPostConfigureOptions>());
        return services;
    }
}
