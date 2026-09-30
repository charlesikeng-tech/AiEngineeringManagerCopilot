using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Actions;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Helpers;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Endpoints;

public sealed class EngineeringActionEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EngineeringActionEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetActions_ShouldReturnLatestReportingPeriod()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricAsync(
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

        await SeedMetricAsync(
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
            $"/teams/{teamId}/actions");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadApiJsonAsync<EngineeringActionsResponse>();

        result.Should().NotBeNull();

        result!.TeamId.Should().Be(teamId);

        result.PeriodStart.Should()
            .Be(new DateOnly(2026, 9, 1));

        result.PeriodEnd.Should()
            .Be(new DateOnly(2026, 9, 30));

        result.Actions.Should().NotBeEmpty();

        result.Actions.Should()
            .OnlyContain(action =>
                action.ReportId == result.ReportId);
    }

    [Fact]
    public async Task GetActions_ShouldUseLatestReportingPeriod_NotLatestCreatedReport()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        // September is deliberately created first.
        await SeedMetricAsync(
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

        // August is deliberately created after September.
        await SeedMetricAsync(
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
            $"/teams/{teamId}/actions");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadApiJsonAsync<EngineeringActionsResponse>();

        result.Should().NotBeNull();

        result!.PeriodStart.Should()
            .Be(new DateOnly(2026, 9, 1));

        result.PeriodEnd.Should()
            .Be(new DateOnly(2026, 9, 30));

        result.ReportId.Should()
            .Be(septemberReport!.Id);

        result.Actions.Should()
            .OnlyContain(action =>
                action.ReportId == septemberReport.Id);
    }

    [Fact]
    public async Task GetActions_ShouldReturnNotFound_WhenTeamDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync(
            $"/teams/{Guid.NewGuid()}/actions");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActions_ShouldReturnNotFound_WhenNoReportExists()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        // Act
        var response = await _client.GetAsync(
            $"/teams/{teamId}/actions");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActions_ShouldRequireAuthentication()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/teams/{teamId}/actions");

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
    public async Task GetActions_ShouldReturnNotFound_WhenTeamBelongsToAnotherUser()
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
            $"/teams/{teamId}/actions");

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task UpdateAction_ShouldUpdateStatusOwnerAndDueDate()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricAsync(
            teamId,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var reportResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        reportResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var actionsResponse = await _client.GetAsync(
            $"/teams/{teamId}/actions");

        actionsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var actions = await actionsResponse.Content
            .ReadApiJsonAsync<EngineeringActionsResponse>();

        actions.Should().NotBeNull();
        actions!.Actions.Should().NotBeEmpty();

        var action = actions.Actions.First();

        var request = new
        {
            Status = ActionStatus.InProgress,
            Owner = "Charles",
            DueDate = new DateOnly(2026, 10, 15)
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/teams/{teamId}/actions/{action.Id}",
            request);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadApiJsonAsync<EngineeringActionResponse>();

        result.Should().NotBeNull();

        result!.Id.Should().Be(action.Id);
        result.Status.Should().Be(ActionStatus.InProgress.ToString());
        result.Owner.Should().Be("Charles");
        result.DueDate.Should()
            .Be(new DateOnly(2026, 10, 15));

        // Verify persistence using a new DbContext scope.
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var persistedAction = await dbContext.EngineeringActions
            .FindAsync(action.Id);

        persistedAction.Should().NotBeNull();

        persistedAction!.Status.Should()
            .Be(ActionStatus.InProgress);

        persistedAction.Owner.Should()
            .Be("Charles");

        persistedAction.DueDate.Should()
            .Be(new DateOnly(2026, 10, 15));
    }
    
    [Fact]
    public async Task UpdateAction_ShouldReturnNotFound_WhenActionDoesNotExist()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        var request = new
        {
            Status = ActionStatus.InProgress,
            Owner = "Charles",
            DueDate = new DateOnly(2026, 10, 15)
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/teams/{teamId}/actions/{Guid.NewGuid()}",
            request);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAction_ShouldRequireAuthentication()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/teams/{teamId}/actions/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new
            {
                Status = ActionStatus.InProgress,
                Owner = "Charles",
                DueDate = new DateOnly(2026, 10, 15)
            })
        };

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
    public async Task UpdateAction_ShouldReturnNotFound_WhenActionBelongsToAnotherTeam()
    {
        // Arrange
        var firstTeamId = await CreateTeamAsync();
        var secondTeamId = await CreateTeamAsync();

        await SeedMetricAsync(
            secondTeamId,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var reportResponse = await _client.PostAsync(
            $"/teams/{secondTeamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        reportResponse.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var actionsResponse = await _client.GetAsync(
            $"/teams/{secondTeamId}/actions");

        actionsResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var actions = await actionsResponse.Content
            .ReadApiJsonAsync<EngineeringActionsResponse>();

        actions.Should().NotBeNull();
        actions!.Actions.Should().NotBeEmpty();

        var action = actions.Actions.First();

        var request = new
        {
            Status = ActionStatus.InProgress,
            Owner = "Charles",
            DueDate = new DateOnly(2026, 10, 15)
        };

        // Act
        // Deliberately use firstTeamId with an action belonging to secondTeamId.
        var response = await _client.PatchAsJsonAsync(
            $"/teams/{firstTeamId}/actions/{action.Id}",
            request);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateTeamAsync()
    {
        var request = new
        {
            Name = $"Action Team {Guid.NewGuid():N}",
            Description = "Action integration test"
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

    private async Task SeedMetricAsync(
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
                DataStatus = MetricDataStatus.Available,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }
}