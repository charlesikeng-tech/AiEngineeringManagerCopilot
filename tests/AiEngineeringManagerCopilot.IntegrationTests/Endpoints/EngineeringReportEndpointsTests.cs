using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Endpoints;

public sealed class EngineeringReportEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EngineeringReportEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    private async Task SeedMetricsWithManyInsightsAsync(Guid teamId)
    {
        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            49);

        await SeedMetricAsync(
            teamId,
            MetricType.PRReviewTime,
            25);

        await SeedMetricAsync(
            teamId,
            MetricType.DeploymentFrequency,
            2);

        await SeedMetricAsync(
            teamId,
            MetricType.ChangeFailureRate,
            25);

        await SeedMetricAsync(
            teamId,
            MetricType.LeadTime,
            73);

        await SeedMetricAsync(
            teamId,
            MetricType.OpenPRs,
            11);

        await SeedMetricAsync(
            teamId,
            MetricType.MergedPRs,
            2);

        await SeedMetricAsync(
            teamId,
            MetricType.BlockedItems,
            6);
    }

    [Fact]
    public async Task GenerateReport_ShouldReturnCreated()
    {
        var teamId = await CreateTeamAsync();

        await SeedAllMetricsAsync(teamId);

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringReportResponse>();

        result.Should().NotBeNull();
        result.Actions.Should().BeEmpty();
        result!.TeamId.Should().Be(teamId);
        result.OverallScore.Should().Be(100);
        result.HealthLevel.Should().Be("Excellent");
        result.ExecutiveSummary.Should().Contain("Excellent");
        result.ExecutiveSummary.Should().Contain("100/100");
        result.Actions.Should().AllSatisfy(action =>
        {
            action.ReportId.Should().Be(result.Id);
            action.Title.Should().NotBeNullOrWhiteSpace();
            action.Description.Should().NotBeNullOrWhiteSpace();
            action.Status.Should().Be("Todo");
        });
        result.PeriodStart.Should().Be(new DateOnly(2026, 9, 1));
        result.PeriodEnd.Should().Be(new DateOnly(2026, 9, 30));
    }

    [Fact]
    public async Task GenerateReport_ShouldPersistReport()
    {
        var teamId = await CreateTeamAsync();

        await SeedAllMetricsAsync(teamId);

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringReportResponse>();

        result.Should().NotBeNull();

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var report = await dbContext.EngineeringReports
            .FindAsync(result!.Id);

        report.Should().NotBeNull();
        report!.TeamId.Should().Be(teamId);
        report.OverallScore.Should().Be(100);
        var actions = await dbContext.EngineeringActions
            .Where(x => x.ReportId == report.Id)
            .ToListAsync();

        actions.Should().AllSatisfy(action =>
        {
            action.ReportId.Should().Be(report.Id);
            action.Status.Should().Be(ActionStatus.Todo);
        });
    }

    [Fact]
    public async Task GenerateReport_ShouldReturnNotFoundForUnknownTeam()
    {
        var unknownTeamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{unknownTeamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReports_ShouldReturnTeamReports()
    {
        var teamId = await CreateTeamAsync();

        await SeedAllMetricsAsync(teamId);

        var septemberResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        septemberResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var augustResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-08-01" +
            "&periodEnd=2026-08-31",
            null);

        augustResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var response = await _client.GetAsync(
            $"/teams/{teamId}/reports");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var reports =
            await response.Content
                .ReadFromJsonAsync<
                    IReadOnlyList<EngineeringReportResponse>>();

        reports.Should().NotBeNull();
        reports!.Count.Should().Be(2);

        reports.Should()
            .OnlyContain(x => x.TeamId == teamId);

        reports.Should().AllSatisfy(report =>
        {
            report.Actions.Should().NotBeNull();

            report.Actions.Should().AllSatisfy(action =>
            {
                action.ReportId.Should().Be(report.Id);
                action.Status.Should().Be("Todo");
            });
        });
    }
    
    [Fact]
    public async Task GetReports_ShouldReturnNoData_WhenReportHasNoMetrics()
    {
        var teamId = await CreateTeamAsync();

        var createResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-08-01" +
            "&periodEnd=2026-08-31",
            null);

        createResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var response = await _client.GetAsync(
            $"/teams/{teamId}/reports");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var reports =
            await response.Content
                .ReadFromJsonAsync<
                    IReadOnlyList<EngineeringReportResponse>>();

        reports.Should().NotBeNull();
        reports.Should().ContainSingle();

        var report = reports!.Single();

        report.OverallScore.Should().Be(0);
        report.DataCoverage.Should().Be(0m);
        report.HealthLevel.Should().Be("No Data");
    }

    [Fact]
    public async Task GetReport_ShouldReturnReport()
    {
        var teamId = await CreateTeamAsync();

        await SeedAllMetricsAsync(teamId);

        var createResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<EngineeringReportResponse>();

        created.Should().NotBeNull();

        var response = await _client.GetAsync(
            $"/teams/{teamId}/reports/{created!.Id}");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringReportResponse>();

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.TeamId.Should().Be(teamId);
        result.OverallScore.Should().Be(100);
        result.HealthLevel.Should().Be("Excellent");
        result.Actions.Should().NotBeNull();

        result.Actions.Should().AllSatisfy(action =>
        {
            action.ReportId.Should().Be(result.Id);
            action.Status.Should().Be("Todo");
        });
    }

    [Fact]
    public async Task GetReport_ShouldReturnNotFoundForUnknownReport()
    {
        var teamId = await CreateTeamAsync();

        var response = await _client.GetAsync(
            $"/teams/{teamId}/reports/{Guid.NewGuid()}");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReport_ShouldNotReturnAnotherTeamsReport()
    {
        var team1Id = await CreateTeamAsync();
        var team2Id = await CreateTeamAsync();

        await SeedAllMetricsAsync(team1Id);

        var createResponse = await _client.PostAsync(
            $"/teams/{team1Id}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        var report =
            await createResponse.Content
                .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        var response = await _client.GetAsync(
            $"/teams/{team2Id}/reports/{report!.Id}");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GenerateReport_ShouldReturnMetricTrendComparedToPreviousPeriod()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            50m,
            new DateOnly(2026, 8, 2),
            new DateOnly(2026, 8, 31));

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            30m,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        // Act
        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var report = await response.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        report!.Trends.Should().ContainSingle();

        var trend = report.Trends.Single();

        trend.MetricType.Should().Be("CycleTime");
        trend.CurrentValue.Should().Be(30m);
        trend.PreviousValue.Should().Be(50m);
        trend.ChangePercentage.Should().Be(-40m);
        trend.Direction.Should().Be("Improving");
    }
    
    [Fact]
    public async Task GetReport_ShouldReturnMetricTrend()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            50m,
            new DateOnly(2026, 8, 2),
            new DateOnly(2026, 8, 31));

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            30m,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var createResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        createResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var createdReport = await createResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        createdReport.Should().NotBeNull();

        // Act
        var response = await _client.GetAsync(
            $"/teams/{teamId}/reports/{createdReport!.Id}");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var report = await response.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();
        report!.Trends.Should().ContainSingle();

        var trend = report.Trends.Single();

        trend.MetricType.Should().Be("CycleTime");
        trend.CurrentValue.Should().Be(30m);
        trend.PreviousValue.Should().Be(50m);
        trend.ChangePercentage.Should().Be(-40m);
        trend.Direction.Should().Be("Improving");
    }
    
    [Fact]
    public async Task GetReports_ShouldReturnMetricTrends()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            50m,
            new DateOnly(2026, 8, 2),
            new DateOnly(2026, 8, 31));

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            30m,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var createResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        createResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        // Act
        var response = await _client.GetAsync(
            $"/teams/{teamId}/reports");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var reports = await response.Content
            .ReadFromJsonAsync<
                IReadOnlyList<EngineeringReportResponse>>();

        reports.Should().NotBeNull();
        reports.Should().ContainSingle();

        var report = reports!.Single();

        report.Trends.Should().ContainSingle();

        var trend = report.Trends.Single();

        trend.MetricType.Should().Be("CycleTime");
        trend.CurrentValue.Should().Be(30m);
        trend.PreviousValue.Should().Be(50m);
        trend.ChangePercentage.Should().Be(-40m);
        trend.Direction.Should().Be("Improving");
    }
    
    [Fact]
    public async Task AnalyzeReport_ShouldIncludeMetricTrendsInPrompt()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            50m,
            new DateOnly(2026, 8, 2),
            new DateOnly(2026, 8, 31));

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            30m,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var generateResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        generateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var report = await generateResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        // Act
        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports/{report!.Id}/analyze",
            null);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var llmProvider = _factory.Services
            .GetRequiredService<ILlmProvider>();

        var fakeLlmProvider =
            llmProvider.Should()
                .BeOfType<FakeLlmProvider>()
                .Subject;

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("Metric trends compared with previous period");

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("CycleTime");

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("50 → 30");

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("-40%");

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("Improving");
    }

    private async Task<Guid> CreateTeamAsync()
    {
        var request = new
        {
            Name = $"Report Team {Guid.NewGuid():N}",
            Description = "Report integration test"
        };

        var response = await _client.PostAsJsonAsync(
            "/teams",
            request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var team =
            await response.Content
                .ReadFromJsonAsync<TeamResponse>();

        team.Should().NotBeNull();

        return team!.Id;
    }

    private async Task SeedAllMetricsAsync(Guid teamId)
    {
        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            8);

        await SeedMetricAsync(
            teamId,
            MetricType.PRReviewTime,
            4);

        await SeedMetricAsync(
            teamId,
            MetricType.DeploymentFrequency,
            20);

        await SeedMetricAsync(
            teamId,
            MetricType.ChangeFailureRate,
            5);

        await SeedMetricAsync(
            teamId,
            MetricType.LeadTime,
            24);

        await SeedMetricAsync(
            teamId,
            MetricType.OpenPRs,
            2);

        await SeedMetricAsync(
            teamId,
            MetricType.MergedPRs,
            20);

        await SeedMetricAsync(
            teamId,
            MetricType.BlockedItems,
            0);
    }

    private async Task SeedMetricAsync(
        Guid teamId,
        MetricType metricType,
        decimal value,
        DateOnly? periodStart = null,
        DateOnly? periodEnd = null)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        dbContext.EngineeringMetrics.Add(
            new EngineeringMetric
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                MetricType = metricType,
                Value = value,
                PeriodStart = periodStart
                              ?? new DateOnly(2026, 9, 1),
                PeriodEnd = periodEnd
                            ?? new DateOnly(2026, 9, 30),
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }
    
    private async Task SeedMetricsWithInsightsAsync(Guid teamId)
    {
        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            48);

        await SeedMetricAsync(
            teamId,
            MetricType.PRReviewTime,
            4);

        await SeedMetricAsync(
            teamId,
            MetricType.DeploymentFrequency,
            20);

        await SeedMetricAsync(
            teamId,
            MetricType.ChangeFailureRate,
            5);

        await SeedMetricAsync(
            teamId,
            MetricType.LeadTime,
            24);

        await SeedMetricAsync(
            teamId,
            MetricType.OpenPRs,
            2);

        await SeedMetricAsync(
            teamId,
            MetricType.MergedPRs,
            20);

        await SeedMetricAsync(
            teamId,
            MetricType.BlockedItems,
            0);
    }
    
    [Fact]
    public async Task GenerateReport_ShouldGenerateActions_WhenInsightsExist()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithInsightsAsync(teamId);

        // Act
        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports/?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        result.Should().NotBeNull();

        Console.WriteLine($"OverallScore = {result.OverallScore}");
        result!.OverallScore.Should().BeGreaterThan(0);

        result.Insights.Should().NotBeEmpty();
        result.Actions.Should().NotBeEmpty();

        result.Actions.Should().AllSatisfy(action =>
        {
            action.ReportId.Should().Be(result.Id);
            action.Status.Should().Be("Todo");
        });
    }
    
    [Fact]
    public async Task GenerateReport_ShouldPersistMaximumThreeActions()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        // Act
        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringReportResponse>();

        result.Should().NotBeNull();

        result!.Insights.Should().HaveCount(8);
        result.Actions.Should().HaveCount(3);

        result.Actions.Should().AllSatisfy(action =>
        {
            action.ReportId.Should().Be(result.Id);
            action.Status.Should().Be("Todo");
        });

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var actions = await dbContext.EngineeringActions
            .Where(x => x.ReportId == result.Id)
            .ToListAsync();

        actions.Should().HaveCount(3);
    }
    
    [Fact]
    public async Task GenerateReport_ShouldReturnActionPriorities()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        // Act
        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringReportResponse>();

        result.Should().NotBeNull();
        result!.Actions.Should().HaveCount(3);

        result.Actions
            .Should()
            .Contain(action =>
                action.Priority == ActionPriority.Critical);

        result.Actions
            .Should()
            .Contain(action =>
                action.Priority == ActionPriority.High);

        result.Actions
            .Should()
            .AllSatisfy(action =>
            {
                action.ReportId.Should().Be(result.Id);
                action.Status.Should().Be("Todo");
            });
    }
    
    [Fact]
    public async Task GenerateReport_ShouldPersistActionPriorities()
    {
        // Arrange
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        // Act
        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringReportResponse>();

        result.Should().NotBeNull();

        // Assert
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var actions = await dbContext.EngineeringActions
            .Where(x => x.ReportId == result!.Id)
            .OrderByDescending(x => x.Priority)
            .ToListAsync();

        actions.Should().HaveCount(3);

        actions.Should().Contain(x =>
            x.Priority == ActionPriority.Critical);

        actions.Should().Contain(x =>
            x.Priority == ActionPriority.High);

        actions.Should().AllSatisfy(action =>
        {
            action.ReportId.Should().Be(result.Id);
            action.Status.Should().Be(ActionStatus.Todo);
        });
    }
    
    [Fact]
    public async Task GenerateReport_ShouldGenerateRisks_WhenMetricsAreUnhealthy()
    {
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await response.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();
        report!.Risks.Should().NotBeEmpty();
        report.Risks.Should().HaveCount(8);
    }
    
    [Fact]
    public async Task GenerateReport_ShouldPersistRisks_WhenMetricsAreUnhealthy()
    {
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await response.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();
        report!.Risks.Should().HaveCount(8);

        var getResponse = await _client.GetAsync(
            $"/teams/{teamId}/reports/{report.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var persistedReport = await getResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        persistedReport.Should().NotBeNull();
        persistedReport!.Risks.Should().HaveCount(8);

        persistedReport.Risks.Should().Contain(r =>
            r.Title == "High change failure rate" &&
            r.Severity == RiskSeverity.Critical);

        persistedReport.Risks.Should().Contain(r =>
            r.Title == "Low deployment frequency" &&
            r.Severity == RiskSeverity.High);
    }
    
    [Fact]
    public async Task GetReport_ShouldNotExposeRisksFromAnotherTeam()
    {
        var team1Id = await CreateTeamAsync();
        var team2Id = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(team1Id);

        var generateResponse = await _client.PostAsync(
            $"/teams/{team1Id}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await generateResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();
        report!.Risks.Should().HaveCount(8);

        var response = await _client.GetAsync(
            $"/teams/{team2Id}/reports/{report.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetReports_ShouldReturnOnlyReportsFromRequestedTeam()
    {
        var team1Id = await CreateTeamAsync();
        var team2Id = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(team1Id);
        await SeedMetricsWithManyInsightsAsync(team2Id);

        var team1GenerateResponse = await _client.PostAsync(
            $"/teams/{team1Id}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        var team2GenerateResponse = await _client.PostAsync(
            $"/teams/{team2Id}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        team1GenerateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        team2GenerateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.GetAsync(
            $"/teams/{team1Id}/reports");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var reports = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<EngineeringReportResponse>>();

        reports.Should().NotBeNull();
        reports.Should().ContainSingle();

        reports![0].TeamId.Should().Be(team1Id);
    }
    
    [Fact]
    public async Task AnalyzeReport_ShouldReturnAIAnalysis()
    {
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        var generateResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await generateResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports/{report!.Id}/analyze",
            null);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var llmProvider = _factory.Services
            .GetRequiredService<ILlmProvider>();

        llmProvider.Should().BeOfType<FakeLlmProvider>();

        var fakeLlmProvider = (FakeLlmProvider)llmProvider;

        fakeLlmProvider.LastPrompt
            .Should()
            .NotBeNullOrWhiteSpace();

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("CycleTime");

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("DeploymentFrequency");

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("ChangeFailureRate");

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("Detected risks");

        fakeLlmProvider.LastPrompt
            .Should()
            .Contain("Existing insights");
        

        var analysis = await response.Content
            .ReadFromJsonAsync<AIAnalysisResult>();

        analysis.Should().NotBeNull();

        analysis!.Summary.Should().NotBeNullOrWhiteSpace();

        analysis.Insights.Should().NotBeEmpty();

        analysis.Actions.Should().NotBeEmpty();

        analysis.Insights.Should().ContainSingle();

        analysis.Insights[0].Category.Should().Be("Delivery");

        analysis.Actions.Should().ContainSingle();

        analysis.Actions[0].Priority.Should().Be(ActionPriority.High);
    }
    
    [Fact]
    public async Task AnalyzeReport_ShouldReturnNotFoundForUnknownReport()
    {
        var teamId = await CreateTeamAsync();

        var unknownReportId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports/{unknownReportId}/analyze",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task AnalyzeReport_ShouldReturnNotFoundForAnotherTeamReport()
    {
        var firstTeamId = await CreateTeamAsync();
        var secondTeamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(firstTeamId);

        var generateResponse = await _client.PostAsync(
            $"/teams/{firstTeamId}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await generateResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        var response = await _client.PostAsync(
            $"/teams/{secondTeamId}/reports/{report!.Id}/analyze",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task AnalyzeReport_ShouldPersistAIAnalysis()
    {
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        var generateResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await generateResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports/{report!.Id}/analyze",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var analysis = await dbContext.AIAnalyses
            .SingleOrDefaultAsync(x => x.ReportId == report.Id);

        analysis.Should().NotBeNull();

        analysis!.Summary
            .Should()
            .NotBeNullOrWhiteSpace();

        analysis.ReportId
            .Should()
            .Be(report.Id);
    }
    
    [Fact]
    public async Task AnalyzeReport_ShouldNotCreateDuplicateAIAnalysis()
    {
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        var generateResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await generateResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        var firstResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports/{report!.Id}/analyze",
            null);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports/{report.Id}/analyze",
            null);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var analyses = await dbContext.AIAnalyses
            .Where(x => x.ReportId == report.Id)
            .ToListAsync();

        analyses.Should().ContainSingle();
    }
    
    [Fact]
    public async Task AnalyzeReport_ShouldPersistAIInsightsAndActions()
    {
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        var generateResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await generateResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports/{report!.Id}/analyze",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var analysis = await dbContext.AIAnalyses
            .SingleAsync(x => x.ReportId == report.Id);

        var insights = await dbContext.AIAnalysisInsights
            .Where(x => x.AIAnalysisId == analysis.Id)
            .ToListAsync();

        var actions = await dbContext.AIAnalysisActions
            .Where(x => x.AIAnalysisId == analysis.Id)
            .ToListAsync();

        insights.Should().ContainSingle();

        insights[0].Category
            .Should()
            .Be("Delivery");

        insights[0].Title
            .Should()
            .Be("Delivery performance needs attention");

        actions.Should().ContainSingle();

        actions[0].Title
            .Should()
            .Be("Review delivery bottlenecks");

        actions[0].Priority
            .Should()
            .Be(ActionPriority.High);
    }
    
    [Fact]
    public async Task AnalyzeReport_ShouldReturnPersistedInsightsAndActionsOnSecondCall()
    {
        var teamId = await CreateTeamAsync();

        await SeedMetricsWithManyInsightsAsync(teamId);

        var generateResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await generateResponse.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        var firstResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports/{report!.Id}/analyze",
            null);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await _client.PostAsync(
            $"/teams/{teamId}/reports/{report.Id}/analyze",
            null);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondAnalysis = await secondResponse.Content
            .ReadFromJsonAsync<AIAnalysisResult>();

        secondAnalysis.Should().NotBeNull();

        secondAnalysis!.Summary
            .Should()
            .Be("The engineering team shows several areas requiring attention.");

        secondAnalysis.Insights.Should().ContainSingle();

        secondAnalysis.Insights[0].Category
            .Should()
            .Be("Delivery");

        secondAnalysis.Insights[0].Title
            .Should()
            .Be("Delivery performance needs attention");

        secondAnalysis.Actions.Should().ContainSingle();

        secondAnalysis.Actions[0].Title
            .Should()
            .Be("Review delivery bottlenecks");

        secondAnalysis.Actions[0].Priority
            .Should()
            .Be(ActionPriority.High);
    }
    
    [Fact]
    public async Task GenerateReport_ShouldCreateEarlyWarningInsight_WhenMetricIsDegrading()
    {
        var teamId = await CreateTeamAsync();

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            10m,
            new DateOnly(2026, 8, 2),
            new DateOnly(2026, 8, 31));

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            20m,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var report = await response.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        report!.Insights
            .Should()
            .ContainSingle(x =>
                x.Title.Contains("Early warning") &&
                x.Title.Contains("CycleTime"));
    }
    
    [Fact]
    public async Task GenerateReport_ShouldNotCreateEarlyWarning_WhenClassicInsightAlreadyExists()
    {
        var teamId = await CreateTeamAsync();

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            10m,
            new DateOnly(2026, 8, 2),
            new DateOnly(2026, 8, 31));

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            30m,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var response = await _client.PostAsync(
            $"/teams/{teamId}/reports" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var report = await response.Content
            .ReadFromJsonAsync<EngineeringReportResponse>();

        report.Should().NotBeNull();

        report!.Insights
            .Should()
            .ContainSingle();

        report.Insights[0].Title
            .Should()
            .Be("Cycle time is too high");

        report.Insights
            .Should()
            .NotContain(x =>
                x.Title.Contains("Early warning"));
    }
}