using System.Net;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AiEngineeringManagerCopilot.IntegrationTests.Health;

public sealed class HealthCheckEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthCheckEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Live_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync(
            "/health/live");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_WhenDatabaseIsAvailable_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync(
            "/health/ready");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task Live_WhenDatabaseHealthCheckFails_ShouldReturnOk()
    {
        // Arrange
        using var factory =
            _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    ReplaceDatabaseHealthCheckWithFailure(services);
                });
            });

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/health/live");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_WhenDatabaseHealthCheckFails_ShouldReturnServiceUnavailable()
    {
        // Arrange
        using var factory =
            _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    ReplaceDatabaseHealthCheckWithFailure(services);
                });
            });

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/health/ready");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.ServiceUnavailable);
    }
    
    private static void ReplaceDatabaseHealthCheckWithFailure(
        IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck(
                "database-test-failure",
                () => HealthCheckResult.Unhealthy(
                    "Database is unavailable."),
                tags: ["ready"]);
    }
}