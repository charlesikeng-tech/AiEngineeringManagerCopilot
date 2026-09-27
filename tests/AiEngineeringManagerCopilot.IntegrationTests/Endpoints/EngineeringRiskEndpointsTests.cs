using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Helpers;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Endpoints;

public sealed class EngineeringRiskEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EngineeringRiskEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRisks_ShouldReturnLatestReportingPeriod()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedUnhealthyMetricAsync(
            teamId,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31));

        var augustResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-08-01" +
            "&periodEnd=2026-08-31",
            null);

        augustResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        await SeedUnhealthyMetricAsync(
            teamId,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var septemberResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        septemberResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        // Act
        var response = await _client.GetAsync(
            $"/teams/{teamId}/risks");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadApiJsonAsync<EngineeringRisksResponse>();

        result.Should().NotBeNull();

        result!.TeamId.Should().Be(teamId);

        result.PeriodStart.Should()
            .Be(new DateOnly(2026, 9, 1));

        result.PeriodEnd.Should()
            .Be(new DateOnly(2026, 9, 30));

        result.Risks.Should().NotBeEmpty();

        result.Risks.Should()
            .OnlyContain(x => x.ReportId == result.ReportId);
    }
    
    [Fact]
    public async Task GetRisks_ShouldUseLatestReportingPeriod_NotLatestCreatedReport()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        // September is created first.
        await SeedUnhealthyMetricAsync(
            teamId,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var septemberResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        septemberResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var septemberReport = await septemberResponse.Content
            .ReadApiJsonAsync<EngineeringReportResponse>();

        septemberReport.Should().NotBeNull();

        // August is deliberately created AFTER September.
        await SeedUnhealthyMetricAsync(
            teamId,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31));

        var augustResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-08-01" +
            "&periodEnd=2026-08-31",
            null);

        augustResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        // Act
        var response = await _client.GetAsync(
            $"/teams/{teamId}/risks");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadApiJsonAsync<EngineeringRisksResponse>();

        result.Should().NotBeNull();

        result!.PeriodStart.Should()
            .Be(new DateOnly(2026, 9, 1));

        result.PeriodEnd.Should()
            .Be(new DateOnly(2026, 9, 30));

        result.ReportId.Should()
            .Be(septemberReport!.Id);

        result.Risks.Should()
            .OnlyContain(risk =>
                risk.ReportId == septemberReport.Id);
    }
    
    [Fact]
    public async Task GetRisks_ShouldReturnNotFound_WhenTeamDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync(
            $"/teams/{Guid.NewGuid()}/risks");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRisks_ShouldReturnNotFound_WhenNoReportExists()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        // Act
        var response = await _client.GetAsync(
            $"/teams/{teamId}/risks");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRisks_ShouldRequireAuthentication()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/teams/{teamId}/risks");

        request.Headers.Add(
            "X-Test-Unauthenticated",
            "true");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task GetRisks_ShouldReturnNotFound_WhenTeamBelongsToAnotherUser()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        await using (var scope =
                     _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            dbContext.Users.Add(
                new User
                {
                    Id = otherUserId,
                    Email = $"other-{Guid.NewGuid():N}@example.com",
                    Name = "Other User",
                    CreatedAt = DateTimeOffset.UtcNow
                });

            dbContext.Teams.Add(
                new Team
                {
                    Id = teamId,
                    OwnerUserId = otherUserId,
                    Name = "Other user's team",
                    CreatedAt = DateTimeOffset.UtcNow
                });

            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(
            $"/teams/{teamId}/risks");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateTeamAsync()
    {
        var request = new
        {
            Name = $"Risk Team {Guid.NewGuid():N}",
            Description = "Risk integration test"
        };

        var response = await _client.PostAsJsonAsync(
            "/teams",
            request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var team = await response.Content
            .ReadApiJsonAsync<TeamResponse>();

        team.Should().NotBeNull();

        return team!.Id;
    }

    private async Task SeedUnhealthyMetricAsync(
        Guid teamId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        dbContext.EngineeringMetrics.Add(
            new EngineeringMetric
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                MetricType = MetricType.CycleTime,
                Value = 49m,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }
}