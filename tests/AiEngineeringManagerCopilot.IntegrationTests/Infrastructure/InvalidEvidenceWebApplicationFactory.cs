using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;

public sealed class InvalidEvidenceWebApplicationFactory
    : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILlmProvider>();

            services.AddSingleton< ILlmProvider, InvalidEvidenceLlmProvider>();
        });
    }
}