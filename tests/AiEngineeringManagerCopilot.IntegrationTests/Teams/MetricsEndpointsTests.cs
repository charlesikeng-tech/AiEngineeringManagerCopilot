using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Teams;

public sealed class MetricsEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MetricsEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    private async Task SeedPullRequestWithReviewAsync(
        Guid teamId,
        TimeSpan reviewDelay)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ExternalId = Random.Shared.NextInt64(1, long.MaxValue),
            Name = "test-repository",
            FullName = "test-org/test-repository",
            Url = "https://github.com/test-org/test-repository",
            DefaultBranch = "main",
            IsActive = true
        };

        dbContext.Repositories.Add(repository);

        var createdAt =
            new DateTimeOffset(
                2026,
                1,
                10,
                10,
                0,
                0,
                TimeSpan.Zero);

        var pullRequest = new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = repository.Id,
            ExternalId = Random.Shared.NextInt64(1, long.MaxValue),
            AuthorExternalId = "test-user",
            Title = "Test PR",
            State = PullRequestState.Merged,
            CreatedAt = createdAt,
            MergedAt = createdAt.AddHours(10),
            ClosedAt = createdAt.AddHours(10)
        };

        dbContext.PullRequests.Add(pullRequest);

        var review = new PullRequestReview
        {
            Id = Guid.NewGuid(),
            PullRequestId = pullRequest.Id,
            ReviewerExternalId = "reviewer",
            SubmittedAt = createdAt.Add(reviewDelay),
            State = PullRequestReviewState.Approved
        };

        dbContext.PullRequestReviews.Add(review);

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedPullRequestAsync(
        Guid teamId,
        TimeSpan mergeDuration,
        bool merged = true,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? mergedAt = null)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ExternalId = Random.Shared.NextInt64(1, long.MaxValue),
            Name = "test-repository",
            FullName = "test-org/test-repository",
            Url = "https://github.com/test-org/test-repository",
            DefaultBranch = "main",
            IsActive = true
        };

        dbContext.Repositories.Add(repository);

        var pullRequestCreatedAt =
            createdAt ??
            new DateTimeOffset(
                2026,
                1,
                10,
                10,
                0,
                0,
                TimeSpan.Zero);

        var pullRequest = new PullRequest
        {
            Id = Guid.NewGuid(),
            RepositoryId = repository.Id,
            ExternalId = Random.Shared.NextInt64(1, long.MaxValue),
            AuthorExternalId = "test-user",
            Title = "Test PR",
            State = merged
                ? PullRequestState.Merged
                : PullRequestState.Open,
            CreatedAt = pullRequestCreatedAt,
            MergedAt = merged
                ? mergedAt ?? pullRequestCreatedAt.Add(mergeDuration)
                : null,
            ClosedAt = merged
                ? mergedAt ?? pullRequestCreatedAt.Add(mergeDuration)
                : null
        };

        dbContext.PullRequests.Add(pullRequest);

        await dbContext.SaveChangesAsync();
    }
    
    private async Task SeedJiraWorkItemAsync(
        Guid teamId,
        bool isBlocked,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? doneAt = null)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var workItem = new JiraWorkItem
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ExternalId = Guid.NewGuid().ToString(),
            Key = $"REC-{Random.Shared.Next(1, 100000)}",
            Summary = "Test Jira work item",
            Status = doneAt.HasValue
                ? "Done"
                : "In Progress",
            AssigneeExternalId = "test-user",
            CreatedAt = createdAt ??
                        new DateTimeOffset(
                            2026, 1, 10, 10, 0, 0, TimeSpan.Zero),
            DoneAt = doneAt,
            IsBlocked = isBlocked
        };

        dbContext.JiraWorkItems.Add(workItem);

        await dbContext.SaveChangesAsync();
    }
    
    private async Task SeedDeploymentAsync(
        Guid teamId,
        string status,
        string date)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ExternalId = Random.Shared.NextInt64(
                1,
                long.MaxValue),
            Name = "test-repository",
            FullName = "test-org/test-repository",
            Url = "https://github.com/test-org/test-repository",
            DefaultBranch = "main",
            IsActive = true
        };

        dbContext.Repositories.Add(repository);

        var deployment = new Deployment
        {
            Id = Guid.NewGuid(),
            RepositoryId = repository.Id,
            ExternalId = Random.Shared.NextInt64(
                1,
                long.MaxValue),
            Environment = "production",
            Status = status,
            DeployedAt = DateTimeOffset.Parse(
                $"{date}T12:00:00+00:00")
        };

        dbContext.Deployments.Add(deployment);

        await dbContext.SaveChangesAsync();
    }
    
    private async Task<TeamResponse> CreateTeamAsync()
    {
        var request = new
        {
            Name = $"Metrics Team {Guid.NewGuid():N}",
            Description = "Metrics integration test"
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

        return team!;
    }

    private async Task SeedPullRequestsAsync(
        Guid teamId,
        params TimeSpan[] durations)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            ExternalId = Random.Shared.NextInt64(1, long.MaxValue),
            Name = "test-repository",
            FullName = "test-org/test-repository",
            Url = "https://github.com/test-org/test-repository",
            DefaultBranch = "main",
            IsActive = true
        };

        dbContext.Repositories.Add(repository);

        var baseDate =
            new DateTimeOffset(
                2026,
                1,
                10,
                10,
                0,
                0,
                TimeSpan.Zero);

        for (var index = 0; index < durations.Length; index++)
        {
            var createdAt =
                baseDate.AddDays(index);

            var pullRequest = new PullRequest
            {
                Id = Guid.NewGuid(),
                RepositoryId = repository.Id,
                ExternalId =
                    Random.Shared.NextInt64(
                        1,
                        long.MaxValue),
                AuthorExternalId = "test-user",
                Title = $"Test PR {index + 1}",
                State = PullRequestState.Merged,
                CreatedAt = createdAt,
                MergedAt = createdAt.Add(durations[index]),
                ClosedAt = createdAt.Add(durations[index])
            };

            dbContext.PullRequests.Add(pullRequest);
        }

        await dbContext.SaveChangesAsync();
    }
    
    private async Task SeedMetricAsync(
        Guid teamId,
        MetricType metricType,
        decimal value)
    {
        await using var scope = _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.EngineeringMetrics.Add(
            new EngineeringMetric
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                MetricType = metricType,
                Value = value,
                PeriodStart = new DateOnly(2026, 9, 1),
                PeriodEnd = new DateOnly(2026, 9, 30),
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CalculateCycleTime_ShouldReturnMetric()
    {
        var team = await CreateTeamAsync();

        await SeedPullRequestsAsync(
            team.Id,
            TimeSpan.FromHours(4),
            TimeSpan.FromHours(10));

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/cycle-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.TeamId.Should().Be(team.Id);
        result.MetricType.Should().Be(MetricType.CycleTime);
        result.Value.Should().Be(7);
        result.PeriodStart.Should().Be(
            new DateOnly(2026, 1, 1));
        result.PeriodEnd.Should().Be(
            new DateOnly(2026, 1, 31));
    }

    [Fact]
    public async Task CalculateCycleTime_ShouldPersistMetric()
    {
        var team = await CreateTeamAsync();

        await SeedPullRequestsAsync(
            team.Id,
            TimeSpan.FromHours(4));

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/cycle-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var metric =
            await dbContext.EngineeringMetrics
                .SingleOrDefaultAsync(x =>
                    x.Id == result!.Id);

        metric.Should().NotBeNull();
        metric!.Value.Should().Be(4);
        metric.MetricType.Should()
            .Be(MetricType.CycleTime);
    }

    [Fact]
    public async Task CalculateCycleTime_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/cycle-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CalculateCycleTime_ShouldReturn400_WhenPeriodIsInvalid()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/cycle-time" +
            "?periodStart=2026-02-01&periodEnd=2026-01-01",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CalculateCycleTime_ShouldReturnZero_WhenNoPullRequestsExist()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/cycle-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.Value.Should().Be(0);
    }
    
    [Fact]
    public async Task CalculatePRReviewTime_ShouldReturnMetric()
    {
        var team = await CreateTeamAsync();

        await SeedPullRequestWithReviewAsync(
            team.Id,
            TimeSpan.FromHours(4));

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/pr-review-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.TeamId.Should().Be(team.Id);
        result.MetricType.Should()
            .Be(MetricType.PRReviewTime);
        result.Value.Should().Be(4);
    }
    
    [Fact]
    public async Task CalculatePRReviewTime_ShouldReturnZero_WhenNoReviewExists()
    {
        var team = await CreateTeamAsync();

        await SeedPullRequestAsync(
            team.Id,
            TimeSpan.FromHours(10));

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/pr-review-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.Value.Should().Be(0);
    }
    
    [Fact]
    public async Task CalculatePRReviewTime_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/pr-review-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CalculateDeploymentFrequency_ShouldReturnMetric()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        await SeedDeploymentAsync(
            teamId,
            "success",
            "2026-01-10");

        await SeedDeploymentAsync(
            teamId,
            "success",
            "2026-01-15");

        var response =
            await _client.PostAsync(
                $"/teams/{teamId}/metrics/deployment-frequency" +
                "?periodStart=2026-01-01" +
                "&periodEnd=2026-01-31",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.MetricType.Should()
            .Be(MetricType.DeploymentFrequency);
        result.Value.Should().Be(2);
    }
    
    [Fact]
    public async Task CalculateDeploymentFrequency_ShouldIgnoreFailedDeployments()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        await SeedDeploymentAsync(
            teamId,
            "success",
            "2026-01-10");

        await SeedDeploymentAsync(
            teamId,
            "failure",
            "2026-01-15");

        var response =
            await _client.PostAsync(
                $"/teams/{teamId}/metrics/deployment-frequency" +
                "?periodStart=2026-01-01" +
                "&periodEnd=2026-01-31",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.Value.Should().Be(1);
    }
    
    [Fact]
    public async Task CalculateDeploymentFrequency_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response =
            await _client.PostAsync(
                $"/teams/{teamId}/metrics/deployment-frequency" +
                "?periodStart=2026-01-01" +
                "&periodEnd=2026-01-31",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CalculateChangeFailureRate_ShouldReturnMetric()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        await SeedDeploymentAsync(
            teamId,
            "success",
            "2026-01-10");

        await SeedDeploymentAsync(
            teamId,
            "failure",
            "2026-01-15");

        await SeedDeploymentAsync(
            teamId,
            "failure",
            "2026-01-20");

        var response =
            await _client.PostAsync(
                $"/teams/{teamId}/metrics/change-failure-rate" +
                "?periodStart=2026-01-01" +
                "&periodEnd=2026-01-31",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.MetricType.Should()
            .Be(MetricType.ChangeFailureRate);
        result.Value.Should().Be(66.67m);
    }
    [Fact]
    public async Task CalculateChangeFailureRate_ShouldReturnZero_WhenNoDeploymentsExist()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        var response =
            await _client.PostAsync(
                $"/teams/{teamId}/metrics/change-failure-rate" +
                "?periodStart=2026-01-01" +
                "&periodEnd=2026-01-31",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.MetricType.Should()
            .Be(MetricType.ChangeFailureRate);
        result.Value.Should().Be(0m);
    }
    
    [Fact]
    public async Task CalculateChangeFailureRate_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response =
            await _client.PostAsync(
                $"/teams/{teamId}/metrics/change-failure-rate" +
                "?periodStart=2026-01-01" +
                "&periodEnd=2026-01-31",
                null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CalculateLeadTime_ShouldReturnMetric()
    {
        var team = await CreateTeamAsync();

        await SeedJiraWorkItemAsync(
            team.Id,
            isBlocked: false,
            createdAt: new DateTimeOffset(
                2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
            doneAt: new DateTimeOffset(
                2026, 1, 10, 18, 0, 0, TimeSpan.Zero));

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/lead-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var metric =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        metric.Should().NotBeNull();
        metric!.MetricType.Should().Be(MetricType.LeadTime);
        metric.Value.Should().Be(10);
    }
    
    [Fact]
    public async Task CalculateLeadTime_ShouldIgnoreIncompleteWorkItems()
    {
        var team = await CreateTeamAsync();

        // Completed Jira item -> 10h Lead Time
        await SeedJiraWorkItemAsync(
            team.Id,
            isBlocked: false,
            createdAt: new DateTimeOffset(
                2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
            doneAt: new DateTimeOffset(
                2026, 1, 10, 18, 0, 0, TimeSpan.Zero));

        // Incomplete Jira item -> ignored
        await SeedJiraWorkItemAsync(
            team.Id,
            isBlocked: false,
            createdAt: new DateTimeOffset(
                2026, 1, 11, 8, 0, 0, TimeSpan.Zero),
            doneAt: null);

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/lead-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var metric =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        metric.Should().NotBeNull();
        metric!.MetricType.Should().Be(MetricType.LeadTime);
        metric.Value.Should().Be(10);
    }
    
    [Fact]
    public async Task CalculateLeadTime_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/lead-time" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CalculateOpenPullRequests_ShouldReturnMetric()
    {
        var team = await CreateTeamAsync();

        await SeedPullRequestAsync(
            team.Id,
            TimeSpan.FromHours(10));

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/open-prs" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var metric =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        metric.Should().NotBeNull();
        metric!.TeamId.Should().Be(team.Id);
        metric.MetricType.Should().Be(MetricType.OpenPRs);
        metric.Value.Should().Be(0);
    }
    
    [Fact]
    public async Task CalculateOpenPullRequests_ShouldCountOpenPullRequests()
    {
        var team = await CreateTeamAsync();

        await SeedPullRequestAsync(
            team.Id,
            TimeSpan.Zero,
            merged: false);

        await SeedPullRequestAsync(
            team.Id,
            TimeSpan.Zero,
            merged: false);

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/open-prs" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var metric =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        metric.Should().NotBeNull();
        metric!.Value.Should().Be(2);
    }
    
    [Fact]
    public async Task CalculateOpenPullRequests_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/open-prs" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CalculateOpenPullRequests_ShouldUsePeriodEndAsSnapshot()
    {
        var team = await CreateTeamAsync();

        // Created before September, still open -> included
        await SeedPullRequestAsync(
            team.Id,
            TimeSpan.Zero,
            merged: false,
            createdAt: new DateTimeOffset(
                2026, 8, 20, 10, 0, 0, TimeSpan.Zero));

        // Created during September, still open -> included
        await SeedPullRequestAsync(
            team.Id,
            TimeSpan.Zero,
            merged: false,
            createdAt: new DateTimeOffset(
                2026, 9, 10, 10, 0, 0, TimeSpan.Zero));

        // Created after September -> excluded
        await SeedPullRequestAsync(
            team.Id,
            TimeSpan.Zero,
            merged: false,
            createdAt: new DateTimeOffset(
                2026, 10, 5, 10, 0, 0, TimeSpan.Zero));

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/open-prs" +
            "?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.MetricType.Should().Be(MetricType.OpenPRs);
        result.Value.Should().Be(2);
    }
    
    [Fact]
    public async Task CalculateMergedPullRequests_ShouldReturnMetric()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;
        
        await SeedPullRequestAsync(
            teamId,
            TimeSpan.FromHours(10),
            merged: true);

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/merged-prs" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.MetricType.Should().Be(MetricType.MergedPRs);
        result.Value.Should().Be(1);
    }
    
    [Fact]
    public async Task CalculateMergedPullRequests_ShouldIgnoreUnmergedPullRequests()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        await SeedPullRequestAsync(
            teamId,
            TimeSpan.Zero,
            merged: false);

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/merged-prs" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.MetricType.Should().Be(MetricType.MergedPRs);
        result.Value.Should().Be(0);
    }
    
    [Fact]
    public async Task CalculateMergedPullRequests_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/merged-prs" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CalculateBlockedItems_ShouldReturnMetric()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        await SeedJiraWorkItemAsync(
            teamId,
            isBlocked: true);

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/blocked-items" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<EngineeringMetricResponse>();
        
        result.Should().NotBeNull();
        result!.MetricType.Should().Be(MetricType.BlockedItems);
        result.Value.Should().Be(1);
    }
    
    [Fact]
    public async Task CalculateBlockedItems_ShouldIgnoreNonBlockedItems()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        await SeedJiraWorkItemAsync(
            teamId,
            isBlocked: false);

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/blocked-items" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();
        result!.MetricType.Should().Be(MetricType.BlockedItems);
        result.Value.Should().Be(0);
    }
    
    [Fact]
    public async Task CalculateBlockedItems_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/metrics/blocked-items" +
            "?periodStart=2026-01-01&periodEnd=2026-01-31",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CalculateHealthScore_ShouldReturnScore()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

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

        var response = await _client.GetAsync(
            $"/teams/{teamId}/health/score" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<
                EngineeringHealthScoreResponse>();

        result.Should().NotBeNull();
        result!.TeamId.Should().Be(teamId);
        result.OverallScore.Should().Be(100);
        result.HealthLevel.Should().Be("Excellent");
        result.DataCoverage.Should().Be(100m);
    }
    
    [Fact]
    public async Task CalculateMergedPullRequests_ShouldUseMergedAtToDeterminePeriod()
    {
        var team = await CreateTeamAsync();

        // Created in August, merged in September -> included
        await SeedPullRequestAsync(
            team.Id,
            TimeSpan.Zero,
            merged: true,
            createdAt: new DateTimeOffset(
                2026, 8, 28, 10, 0, 0, TimeSpan.Zero),
            mergedAt: new DateTimeOffset(
                2026, 9, 3, 10, 0, 0, TimeSpan.Zero));

        // Created in September, merged in October -> excluded
        await SeedPullRequestAsync(
            team.Id,
            TimeSpan.Zero,
            merged: true,
            createdAt: new DateTimeOffset(
                2026, 9, 3, 10, 0, 0, TimeSpan.Zero),
            mergedAt: new DateTimeOffset(
                2026, 10, 1, 10, 0, 0, TimeSpan.Zero));

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/merged-prs" +
            "?periodStart=2026-09-01&periodEnd=2026-09-30",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringMetricResponse>();

        result.Should().NotBeNull();

        result!.MetricType.Should()
            .Be(MetricType.MergedPRs);

        result.Value.Should().Be(1);
    }
    
    [Fact]
    public async Task CalculateHealthScore_ShouldReturnPartialDataCoverage()
    {
        var team = await CreateTeamAsync();
        var teamId = team.Id;

        await SeedMetricAsync(
            teamId,
            MetricType.CycleTime,
            8m);

        await SeedMetricAsync(
            teamId,
            MetricType.BlockedItems,
            0m);

        var response = await _client.GetAsync(
            $"/teams/{teamId}/health/score" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<EngineeringHealthScoreResponse>();

        result.Should().NotBeNull();

        result!.OverallScore.Should().Be(100);
        result.HealthLevel.Should().Be("Excellent");
        result.DataCoverage.Should().Be(30m);
    }
    
}