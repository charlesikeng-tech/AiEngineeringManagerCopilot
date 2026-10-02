using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Security;

public sealed class CorsTests
{
    [Fact]
    public async Task AllowedOrigin_ShouldReturnAccessControlAllowOriginHeader()
    {
        // Arrange
        using var factory =
            new CustomWebApplicationFactory()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.Configure<CorsOptions>(options =>
                        {
                            options.AddDefaultPolicy(policy =>
                            {
                                policy
                                    .WithOrigins("http://localhost:4200")
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                            });
                        });
                    });
                });

        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/health/live");

        request.Headers.Add(
            "Origin",
            "http://localhost:4200");

        // Act
        using var response =
            await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        response.Headers
            .TryGetValues(
                "Access-Control-Allow-Origin",
                out var values)
            .Should()
            .BeTrue();

        values.Should()
            .ContainSingle()
            .Which.Should()
            .Be("http://localhost:4200");
    }

    [Fact]
    public async Task UnknownOrigin_ShouldNotReturnAccessControlAllowOriginHeader()
    {
        // Arrange
        using var factory =
            new CustomWebApplicationFactory()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.Configure<CorsOptions>(options =>
                        {
                            options.AddDefaultPolicy(policy =>
                            {
                                policy
                                    .WithOrigins("http://localhost:4200")
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                            });
                        });
                    });
                });

        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/health/live");

        request.Headers.Add(
            "Origin",
            "https://unauthorized.example.com");

        // Act
        using var response =
            await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        response.Headers
            .Contains("Access-Control-Allow-Origin")
            .Should()
            .BeFalse();
    }
}