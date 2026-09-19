using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Teams;

public sealed class GitHubSyncEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GitHubSyncEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<TeamResponse> CreateTeamAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/teams",
            new CreateTeamRequest(
                $"Team-{Guid.NewGuid():N}",
                null));

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        var team = await response.Content
            .ReadFromJsonAsync<TeamResponse>();

        team.Should().NotBeNull();

        return team!;
    }

    private async Task CreateGitHubConnectionAsync(
        Guid teamId,
        string organization = "my-company",
        string accessToken = "secret-token")
    {
        var response = await _client.PostAsJsonAsync(
            $"/teams/{teamId}/github",
            new CreateGitHubConnectionRequest(
                organization,
                accessToken));

        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);
    }

    private FakeGitHubClient GetFakeGitHubClient()
    {
        return _factory.Services
            .GetRequiredService<FakeGitHubClient>();
    }

    [Fact]
    public async Task SyncGitHubRepositories_ShouldCreateRepositories()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main"),

            new GitHubRepository(
                1002,
                "frontend",
                "my-company/frontend",
                "https://github.com/my-company/frontend",
                "main")
        ];

        // Act
        var response = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<GitHubSyncResponse>();

        result.Should().NotBeNull();
        result!.Synchronized.Should().Be(2);
        result.Created.Should().Be(2);
        result.Updated.Should().Be(0);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldUpdateExistingRepositories()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main")
        ];

        // First sync: creates the repository.
        var firstSyncResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        firstSyncResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var firstResult =
            await firstSyncResponse.Content
                .ReadFromJsonAsync<GitHubSyncResponse>();

        firstResult.Should().NotBeNull();
        firstResult!.Created.Should().Be(1);
        firstResult.Updated.Should().Be(0);

        // Change data returned by GitHub.
        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend-api",
                "my-company/backend-api",
                "https://github.com/my-company/backend-api",
                "develop")
        ];

        // Second sync: should update the existing repository.
        var secondSyncResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        secondSyncResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var secondResult =
            await secondSyncResponse.Content
                .ReadFromJsonAsync<GitHubSyncResponse>();

        secondResult.Should().NotBeNull();
        secondResult!.Synchronized.Should().Be(1);
        secondResult.Created.Should().Be(0);
        secondResult.Updated.Should().Be(1);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldBeIdempotent()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main"),

            new GitHubRepository(
                1002,
                "frontend",
                "my-company/frontend",
                "https://github.com/my-company/frontend",
                "main")
        ];

        // Act - first synchronization
        var firstResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var firstResult =
            await firstResponse.Content
                .ReadFromJsonAsync<GitHubSyncResponse>();

        // Act - second synchronization with exactly the same data
        var secondResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var secondResult =
            await secondResponse.Content
                .ReadFromJsonAsync<GitHubSyncResponse>();

        // Assert
        firstResult.Should().NotBeNull();
        firstResult!.Synchronized.Should().Be(2);
        firstResult.Created.Should().Be(2);
        firstResult.Updated.Should().Be(0);

        secondResult.Should().NotBeNull();
        secondResult!.Synchronized.Should().Be(2);
        secondResult.Created.Should().Be(0);
        secondResult.Updated.Should().Be(2);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldReturn404_WhenTeamDoesNotExist()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.PostAsync(
            $"/teams/{teamId}/github/sync",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldReturn404_WhenGitHubIsNotConfigured()
    {
        var team = await CreateTeamAsync();

        var response = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        response.StatusCode.Should()
            .Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task SyncGitHubRepositories_ShouldDecryptAndPassAccessTokenToGitHubClient()
    {
        // Arrange
        var team = await CreateTeamAsync();

        const string accessToken = "super-secret-github-token";

        await CreateGitHubConnectionAsync(
            team.Id,
            "my-company",
            accessToken);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main")
        ];

        // Act
        var response = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        fakeGitHubClient.ReceivedOrganization
            .Should()
            .Be("my-company");

        fakeGitHubClient.ReceivedAccessToken
            .Should()
            .Be(accessToken);

        var body = await response.Content.ReadAsStringAsync();

        body.Should()
            .NotContain(accessToken);
    }
    
    [Fact]
    public async Task SyncGitHubPullRequests_ShouldCreateAndUpdateWithoutDuplicates()
{
    // Arrange
    var team = await CreateTeamAsync();

    await CreateGitHubConnectionAsync(team.Id);

    var fakeGitHubClient = GetFakeGitHubClient();

    fakeGitHubClient.Repositories =
    [
        new GitHubRepository(
            1001,
            "backend",
            "my-company/backend",
            "https://github.com/my-company/backend",
            "main")
    ];

    fakeGitHubClient.PullRequestsByRepository["backend"] =
    [
        new GitHubPullRequest(
            5001,
            42,
            "Initial pull request",
            "98765",
            "open",
            new DateTimeOffset(
                2026, 9, 1, 10, 0, 0,
                TimeSpan.Zero),
            null,
            null)
    ];

    // Act - first sync
    var firstResponse = await _client.PostAsync(
        $"/teams/{team.Id}/github/sync",
        null);

    firstResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    Guid pullRequestId;

    // Assert - PR created
    using (var scope = _factory.Services.CreateScope())
    {
        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository = await dbContext.Repositories
            .SingleAsync(
                x =>
                    x.TeamId == team.Id &&
                    x.ExternalId == 1001);

        var pullRequests = await dbContext.PullRequests
            .Where(x =>
                x.RepositoryId == repository.Id &&
                x.ExternalId == 5001)
            .ToListAsync();

        pullRequests.Should().HaveCount(1);

        var pullRequest = pullRequests.Single();

        pullRequestId = pullRequest.Id;

        pullRequest.Title.Should()
            .Be("Initial pull request");

        pullRequest.AuthorExternalId.Should()
            .Be("98765");

        pullRequest.State.Should()
            .Be(PullRequestState.Open);

        pullRequest.MergedAt.Should()
            .BeNull();

        pullRequest.ClosedAt.Should()
            .BeNull();
    }

    // GitHub now returns the same PR as merged.
    var mergedAt = new DateTimeOffset(
        2026, 9, 3, 14, 30, 0,
        TimeSpan.Zero);

    fakeGitHubClient.PullRequestsByRepository["backend"] =
    [
        new GitHubPullRequest(
            5001,
            42,
            "Updated pull request",
            "98765",
            "closed",
            new DateTimeOffset(
                2026, 9, 1, 10, 0, 0,
                TimeSpan.Zero),
            mergedAt,
            mergedAt)
    ];

    // Act - second sync
    var secondResponse = await _client.PostAsync(
        $"/teams/{team.Id}/github/sync",
        null);

    secondResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    // Assert - same PR updated, no duplicate.
    using (var scope = _factory.Services.CreateScope())
    {
        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var pullRequests =
            await dbContext.PullRequests
                .Join(
                    dbContext.Repositories,
                    pullRequest => pullRequest.RepositoryId,
                    repository => repository.Id,
                    (pullRequest, repository) => new
                    {
                        PullRequest = pullRequest,
                        repository.TeamId
                    })
                .Where(x =>
                    x.TeamId == team.Id &&
                    x.PullRequest.ExternalId == 5001)
                .Select(x => x.PullRequest)
                .ToListAsync();

        pullRequests.Should().HaveCount(1);

        var pullRequest = pullRequests.Single();

        pullRequest.Id.Should()
            .Be(pullRequestId);

        pullRequest.Title.Should()
            .Be("Updated pull request");

        pullRequest.State.Should()
            .Be(PullRequestState.Merged);

        pullRequest.MergedAt.Should()
            .Be(mergedAt);

        pullRequest.ClosedAt.Should()
            .Be(mergedAt);
    }
}
    
    [Fact]
    public async Task SyncGitHubPullRequestReviews_ShouldCreateAndUpdateWithoutDuplicates()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main")
        ];

        fakeGitHubClient.PullRequestsByRepository["backend"] =
        [
            new GitHubPullRequest(
                5001,
                42,
                "Add recommendation endpoint",
                "12345",
                "open",
                new DateTimeOffset(
                    2026, 9, 1, 10, 0, 0,
                    TimeSpan.Zero),
                null,
                null)
        ];

        fakeGitHubClient.ReviewsByPullRequestNumber[42] =
        [
            new GitHubPullRequestReview(
                7001,
                "67890",
                "APPROVED",
                new DateTimeOffset(
                    2026, 9, 2, 14, 0, 0,
                    TimeSpan.Zero))
        ];

        // Act - first sync
        var firstResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        firstResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        Guid reviewId;

        // Assert - review created
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var repository = await dbContext.Repositories
                .SingleAsync(x =>
                    x.TeamId == team.Id &&
                    x.ExternalId == 1001);

            var pullRequest = await dbContext.PullRequests
                .SingleAsync(x =>
                    x.RepositoryId == repository.Id &&
                    x.ExternalId == 5001);

            var reviews = await dbContext.PullRequestReviews
                .Where(x =>
                    x.PullRequestId == pullRequest.Id &&
                    x.ExternalId == 7001)
                .ToListAsync();

            reviews.Should().HaveCount(1);

            var review = reviews.Single();

            reviewId = review.Id;

            review.ReviewerExternalId.Should()
                .Be("67890");

            review.State.Should()
                .Be(PullRequestReviewState.Approved);

            review.SubmittedAt.Should()
                .Be(new DateTimeOffset(
                    2026, 9, 2, 14, 0, 0,
                    TimeSpan.Zero));
        }

        // GitHub now returns the same review with a different state.
        var updatedSubmittedAt =
            new DateTimeOffset(
                2026, 9, 2, 15, 0, 0,
                TimeSpan.Zero);

        fakeGitHubClient.ReviewsByPullRequestNumber[42] =
        [
            new GitHubPullRequestReview(
                7001,
                "67890",
                "CHANGES_REQUESTED",
                updatedSubmittedAt)
        ];

        // Act - second sync
        var secondResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        secondResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        // Assert - same review updated, no duplicate.
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var repository = await dbContext.Repositories
                .SingleAsync(x =>
                    x.TeamId == team.Id &&
                    x.ExternalId == 1001);

            var pullRequest = await dbContext.PullRequests
                .SingleAsync(x =>
                    x.RepositoryId == repository.Id &&
                    x.ExternalId == 5001);

            var reviews = await dbContext.PullRequestReviews
                .Where(x =>
                    x.PullRequestId == pullRequest.Id &&
                    x.ExternalId == 7001)
                .ToListAsync();

            reviews.Should().HaveCount(1);

            var review = reviews.Single();

            review.Id.Should()
                .Be(reviewId);

            review.ReviewerExternalId.Should()
                .Be("67890");

            review.State.Should()
                .Be(PullRequestReviewState.ChangesRequested);

            review.SubmittedAt.Should()
                .Be(updatedSubmittedAt);
        }
    }
    
    [Fact]
    public async Task GitHubSync_ShouldFeedPRReviewTimeMetric()
{
    // Arrange
    var team = await CreateTeamAsync();

    await CreateGitHubConnectionAsync(team.Id);

    var fakeGitHubClient = GetFakeGitHubClient();

    fakeGitHubClient.Repositories =
    [
        new GitHubRepository(
            1001,
            "backend",
            "my-company/backend",
            "https://github.com/my-company/backend",
            "main")
    ];

    var createdAt = new DateTimeOffset(
        2026, 9, 1, 10, 0, 0,
        TimeSpan.Zero);

    var reviewedAt = new DateTimeOffset(
        2026, 9, 2, 14, 0, 0,
        TimeSpan.Zero);

    var mergedAt = new DateTimeOffset(
        2026, 9, 3, 10, 0, 0,
        TimeSpan.Zero);

    fakeGitHubClient.PullRequestsByRepository["backend"] =
    [
        new GitHubPullRequest(
            5001,
            42,
            "Add recommendation endpoint",
            "12345",
            "closed",
            createdAt,
            mergedAt,
            mergedAt)
    ];

    fakeGitHubClient.ReviewsByPullRequestNumber[42] =
    [
        new GitHubPullRequestReview(
            7001,
            "67890",
            "APPROVED",
            reviewedAt)
    ];

    // Act - synchronize GitHub data
    var syncResponse = await _client.PostAsync(
        $"/teams/{team.Id}/github/sync",
        null);

    syncResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    // Act - calculate PR Review Time
    var metricResponse = await _client.PostAsync(
        $"/teams/{team.Id}/metrics/pr-review-time" +
        "?periodStart=2026-09-01" +
        "&periodEnd=2026-09-30",
        null);

    // Assert
    metricResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    var metric = await metricResponse.Content
        .ReadFromJsonAsync<EngineeringMetricResponse>();

    metric.Should().NotBeNull();

    metric!.TeamId.Should()
        .Be(team.Id);

    metric.MetricType.Should()
        .Be(MetricType.PRReviewTime);

    metric.Value.Should()
        .Be(28m);

    metric.PeriodStart.Should()
        .Be(new DateOnly(2026, 9, 1));

    metric.PeriodEnd.Should()
        .Be(new DateOnly(2026, 9, 30));
}
    
    [Fact]
    public async Task SyncGitHubDeployments_ShouldCreateAndUpdateWithoutDuplicates()
{
    // Arrange
    var team = await CreateTeamAsync();

    await CreateGitHubConnectionAsync(team.Id);

    var fakeGitHubClient = GetFakeGitHubClient();

    fakeGitHubClient.Repositories =
    [
        new GitHubRepository(
            1001,
            "backend",
            "my-company/backend",
            "https://github.com/my-company/backend",
            "main")
    ];

    var deployedAt = new DateTimeOffset(
        2026, 9, 10, 14, 0, 0,
        TimeSpan.Zero);

    fakeGitHubClient.DeploymentsByRepository["backend"] =
    [
        new GitHubDeployment(
            8001,
            "production",
            deployedAt)
    ];

    fakeGitHubClient.DeploymentStatusesByDeploymentId[8001] =
    [
        new GitHubDeploymentStatus(
            9001,
            "in_progress",
            new DateTimeOffset(
                2026, 9, 10, 14, 5, 0,
                TimeSpan.Zero))
    ];

    // Act - first sync
    var firstResponse = await _client.PostAsync(
        $"/teams/{team.Id}/github/sync",
        null);

    firstResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    Guid deploymentId;

    // Assert - deployment created
    using (var scope = _factory.Services.CreateScope())
    {
        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository = await dbContext.Repositories
            .SingleAsync(x =>
                x.TeamId == team.Id &&
                x.ExternalId == 1001);

        var deployments = await dbContext.Deployments
            .Where(x =>
                x.RepositoryId == repository.Id &&
                x.ExternalId == 8001)
            .ToListAsync();

        deployments.Should().HaveCount(1);

        var deployment = deployments.Single();

        deploymentId = deployment.Id;

        deployment.Environment.Should()
            .Be("production");

        deployment.Status.Should()
            .Be("in_progress");

        deployment.DeployedAt.Should()
            .Be(deployedAt);
    }

    // GitHub now reports the same deployment as successful.
    fakeGitHubClient.DeploymentStatusesByDeploymentId[8001] =
    [
        new GitHubDeploymentStatus(
            9001,
            "in_progress",
            new DateTimeOffset(
                2026, 9, 10, 14, 5, 0,
                TimeSpan.Zero)),

        new GitHubDeploymentStatus(
            9002,
            "success",
            new DateTimeOffset(
                2026, 9, 10, 14, 10, 0,
                TimeSpan.Zero))
    ];

    // Act - second sync
    var secondResponse = await _client.PostAsync(
        $"/teams/{team.Id}/github/sync",
        null);

    secondResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    // Assert - same deployment updated, no duplicate.
    using (var scope = _factory.Services.CreateScope())
    {
        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var repository = await dbContext.Repositories
            .SingleAsync(x =>
                x.TeamId == team.Id &&
                x.ExternalId == 1001);

        var deployments = await dbContext.Deployments
            .Where(x =>
                x.RepositoryId == repository.Id &&
                x.ExternalId == 8001)
            .ToListAsync();

        deployments.Should().HaveCount(1);

        var deployment = deployments.Single();

        deployment.Id.Should()
            .Be(deploymentId);

        deployment.Environment.Should()
            .Be("production");

        deployment.Status.Should()
            .Be("success");

        deployment.DeployedAt.Should()
            .Be(deployedAt);
    }
}
    
    [Fact]
    public async Task GitHubSync_ShouldFeedDeploymentFrequencyMetric()
{
    // Arrange
    var team = await CreateTeamAsync();

    await CreateGitHubConnectionAsync(team.Id);

    var fakeGitHubClient = GetFakeGitHubClient();

    fakeGitHubClient.Repositories =
    [
        new GitHubRepository(
            1001,
            "backend",
            "my-company/backend",
            "https://github.com/my-company/backend",
            "main")
    ];

    fakeGitHubClient.DeploymentsByRepository["backend"] =
    [
        new GitHubDeployment(
            8001,
            "production",
            new DateTimeOffset(
                2026, 9, 10, 10, 0, 0,
                TimeSpan.Zero)),

        new GitHubDeployment(
            8002,
            "production",
            new DateTimeOffset(
                2026, 9, 15, 14, 0, 0,
                TimeSpan.Zero)),

        new GitHubDeployment(
            8003,
            "production",
            new DateTimeOffset(
                2026, 9, 20, 9, 0, 0,
                TimeSpan.Zero))
    ];

    fakeGitHubClient.DeploymentStatusesByDeploymentId[8001] =
    [
        new GitHubDeploymentStatus(
            9001,
            "success",
            new DateTimeOffset(
                2026, 9, 10, 10, 5, 0,
                TimeSpan.Zero))
    ];

    fakeGitHubClient.DeploymentStatusesByDeploymentId[8002] =
    [
        new GitHubDeploymentStatus(
            9002,
            "success",
            new DateTimeOffset(
                2026, 9, 15, 14, 5, 0,
                TimeSpan.Zero))
    ];

    fakeGitHubClient.DeploymentStatusesByDeploymentId[8003] =
    [
        new GitHubDeploymentStatus(
            9003,
            "failure",
            new DateTimeOffset(
                2026, 9, 20, 9, 5, 0,
                TimeSpan.Zero))
    ];

    // Act - synchronize GitHub
    var syncResponse = await _client.PostAsync(
        $"/teams/{team.Id}/github/sync",
        null);

    syncResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    // Act - calculate Deployment Frequency
    var metricResponse = await _client.PostAsync(
        $"/teams/{team.Id}/metrics/deployment-frequency" +
        "?periodStart=2026-09-01" +
        "&periodEnd=2026-09-30",
        null);

    // Assert
    metricResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    var metric = await metricResponse.Content
        .ReadFromJsonAsync<EngineeringMetricResponse>();

    metric.Should().NotBeNull();

    metric!.TeamId.Should()
        .Be(team.Id);

    metric.MetricType.Should()
        .Be(MetricType.DeploymentFrequency);

    metric.Value.Should()
        .Be(2m);

    metric.PeriodStart.Should()
        .Be(new DateOnly(2026, 9, 1));

    metric.PeriodEnd.Should()
        .Be(new DateOnly(2026, 9, 30));
}
    
    [Fact]
    public async Task GitHubSync_ShouldFeedChangeFailureRateMetric()
{
    // Arrange
    var team = await CreateTeamAsync();

    await CreateGitHubConnectionAsync(team.Id);

    var fakeGitHubClient = GetFakeGitHubClient();

    fakeGitHubClient.Repositories =
    [
        new GitHubRepository(
            1001,
            "backend",
            "my-company/backend",
            "https://github.com/my-company/backend",
            "main")
    ];

    fakeGitHubClient.DeploymentsByRepository["backend"] =
    [
        new GitHubDeployment(
            8001,
            "production",
            new DateTimeOffset(
                2026, 9, 10, 10, 0, 0,
                TimeSpan.Zero)),

        new GitHubDeployment(
            8002,
            "production",
            new DateTimeOffset(
                2026, 9, 15, 14, 0, 0,
                TimeSpan.Zero)),

        new GitHubDeployment(
            8003,
            "production",
            new DateTimeOffset(
                2026, 9, 20, 9, 0, 0,
                TimeSpan.Zero))
    ];

    fakeGitHubClient.DeploymentStatusesByDeploymentId[8001] =
    [
        new GitHubDeploymentStatus(
            9001,
            "success",
            new DateTimeOffset(
                2026, 9, 10, 10, 5, 0,
                TimeSpan.Zero))
    ];

    fakeGitHubClient.DeploymentStatusesByDeploymentId[8002] =
    [
        new GitHubDeploymentStatus(
            9002,
            "success",
            new DateTimeOffset(
                2026, 9, 15, 14, 5, 0,
                TimeSpan.Zero))
    ];

    fakeGitHubClient.DeploymentStatusesByDeploymentId[8003] =
    [
        new GitHubDeploymentStatus(
            9003,
            "failure",
            new DateTimeOffset(
                2026, 9, 20, 9, 5, 0,
                TimeSpan.Zero))
    ];

    // Act - synchronize GitHub
    var syncResponse = await _client.PostAsync(
        $"/teams/{team.Id}/github/sync",
        null);

    syncResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    // Act - calculate Change Failure Rate
    var metricResponse = await _client.PostAsync(
        $"/teams/{team.Id}/metrics/change-failure-rate" +
        "?periodStart=2026-09-01" +
        "&periodEnd=2026-09-30",
        null);

    // Assert
    metricResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    var metric = await metricResponse.Content
        .ReadFromJsonAsync<EngineeringMetricResponse>();

    metric.Should().NotBeNull();

    metric!.TeamId.Should()
        .Be(team.Id);

    metric.MetricType.Should()
        .Be(MetricType.ChangeFailureRate);

    metric.Value.Should()
        .Be(33.33m);

    metric.PeriodStart.Should()
        .Be(new DateOnly(2026, 9, 1));

    metric.PeriodEnd.Should()
        .Be(new DateOnly(2026, 9, 30));
}
    
    [Fact]
    public async Task GitHubSync_ShouldFeedCycleTimeMetric()
{
    // Arrange
    var team = await CreateTeamAsync();

    await CreateGitHubConnectionAsync(team.Id);

    var fakeGitHubClient = GetFakeGitHubClient();
    
    fakeGitHubClient.Reset();

    fakeGitHubClient.Repositories =
    [
        new GitHubRepository(
            1001,
            "backend",
            "my-company/backend",
            "https://github.com/my-company/backend",
            "main")
    ];

    fakeGitHubClient.PullRequestsByRepository["backend"] =
    [
        new GitHubPullRequest(
            5001,
            42,
            "First feature",
            "10001",
            "closed",
            new DateTimeOffset(
                2026, 9, 10, 10, 0, 0,
                TimeSpan.Zero),
            new DateTimeOffset(
                2026, 9, 11, 10, 0, 0,
                TimeSpan.Zero),
            new DateTimeOffset(
                2026, 9, 11, 10, 0, 0,
                TimeSpan.Zero)),

        new GitHubPullRequest(
            5002,
            43,
            "Second feature",
            "10002",
            "closed",
            new DateTimeOffset(
                2026, 9, 15, 10, 0, 0,
                TimeSpan.Zero),
            new DateTimeOffset(
                2026, 9, 17, 10, 0, 0,
                TimeSpan.Zero),
            new DateTimeOffset(
                2026, 9, 17, 10, 0, 0,
                TimeSpan.Zero))
    ];

    // Act - synchronize GitHub
    var syncResponse = await _client.PostAsync(
        $"/teams/{team.Id}/github/sync",
        null);

    syncResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    // Act - calculate Cycle Time
    var metricResponse = await _client.PostAsync(
        $"/teams/{team.Id}/metrics/cycle-time" +
        "?periodStart=2026-09-01" +
        "&periodEnd=2026-09-30",
        null);

    // Assert
    metricResponse.StatusCode.Should()
        .Be(HttpStatusCode.OK);

    var metric = await metricResponse.Content
        .ReadFromJsonAsync<EngineeringMetricResponse>();

    metric.Should().NotBeNull();

    metric!.TeamId.Should()
        .Be(team.Id);

    metric.MetricType.Should()
        .Be(MetricType.CycleTime);

    metric.Value.Should()
        .Be(36m);

    metric.PeriodStart.Should()
        .Be(new DateOnly(2026, 9, 1));

    metric.PeriodEnd.Should()
        .Be(new DateOnly(2026, 9, 30));
}
    
    [Fact]
    public async Task GitHubSync_ShouldFeedLeadTimeMetric()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Reset();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main")
        ];

        fakeGitHubClient.PullRequestsByRepository["backend"] =
        [
            new GitHubPullRequest(
                5001,
                42,
                "First feature",
                "10001",
                "closed",
                new DateTimeOffset(
                    2026, 9, 10, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 11, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 11, 10, 0, 0,
                    TimeSpan.Zero)),

            new GitHubPullRequest(
                5002,
                43,
                "Second feature",
                "10002",
                "closed",
                new DateTimeOffset(
                    2026, 9, 15, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 17, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 17, 10, 0, 0,
                    TimeSpan.Zero))
        ];

        // Act - synchronize GitHub
        var syncResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        syncResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        // Act - calculate Lead Time
        var metricResponse = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/lead-time" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        // Assert
        metricResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var metric = await metricResponse.Content
            .ReadFromJsonAsync<EngineeringMetricResponse>();

        metric.Should().NotBeNull();

        metric!.TeamId.Should()
            .Be(team.Id);

        metric.MetricType.Should()
            .Be(MetricType.LeadTime);

        metric.Value.Should()
            .Be(36m);

        metric.PeriodStart.Should()
            .Be(new DateOnly(2026, 9, 1));

        metric.PeriodEnd.Should()
            .Be(new DateOnly(2026, 9, 30));
    }
    
    [Fact]
    public async Task GitHubSync_ShouldFeedOpenPullRequestsMetric()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Reset();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main")
        ];

        fakeGitHubClient.PullRequestsByRepository["backend"] =
        [
            new GitHubPullRequest(
                5001,
                42,
                "First open PR",
                "10001",
                "open",
                new DateTimeOffset(
                    2026, 9, 10, 10, 0, 0,
                    TimeSpan.Zero),
                null,
                null),

            new GitHubPullRequest(
                5002,
                43,
                "Second open PR",
                "10002",
                "open",
                new DateTimeOffset(
                    2026, 9, 15, 10, 0, 0,
                    TimeSpan.Zero),
                null,
                null),

            new GitHubPullRequest(
                5003,
                44,
                "Merged PR",
                "10003",
                "closed",
                new DateTimeOffset(
                    2026, 9, 20, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 21, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 21, 10, 0, 0,
                    TimeSpan.Zero))
        ];

        // Act - synchronize GitHub
        var syncResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        syncResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        // Act - calculate Open PRs
        var metricResponse = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/open-prs" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        // Assert
        metricResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var metric = await metricResponse.Content
            .ReadFromJsonAsync<EngineeringMetricResponse>();

        metric.Should().NotBeNull();

        metric!.TeamId.Should()
            .Be(team.Id);

        metric.MetricType.Should()
            .Be(MetricType.OpenPRs);

        metric.Value.Should()
            .Be(2m);

        metric.PeriodStart.Should()
            .Be(new DateOnly(2026, 9, 1));

        metric.PeriodEnd.Should()
            .Be(new DateOnly(2026, 9, 30));
        }
    
    [Fact]
    public async Task GitHubSync_ShouldFeedMergedPullRequestsMetric()
    {
        // Arrange
        var team = await CreateTeamAsync();

        await CreateGitHubConnectionAsync(team.Id);

        var fakeGitHubClient = GetFakeGitHubClient();

        fakeGitHubClient.Reset();

        fakeGitHubClient.Repositories =
        [
            new GitHubRepository(
                1001,
                "backend",
                "my-company/backend",
                "https://github.com/my-company/backend",
                "main")
        ];

        fakeGitHubClient.PullRequestsByRepository["backend"] =
        [
            new GitHubPullRequest(
                5001,
                42,
                "First merged PR",
                "10001",
                "closed",
                new DateTimeOffset(
                    2026, 9, 10, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 11, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 11, 10, 0, 0,
                    TimeSpan.Zero)),

            new GitHubPullRequest(
                5002,
                43,
                "Second merged PR",
                "10002",
                "closed",
                new DateTimeOffset(
                    2026, 9, 15, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 17, 10, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 9, 17, 10, 0, 0,
                    TimeSpan.Zero)),

            new GitHubPullRequest(
                5003,
                44,
                "Open PR",
                "10003",
                "open",
                new DateTimeOffset(
                    2026, 9, 20, 10, 0, 0,
                    TimeSpan.Zero),
                null,
                null)
        ];

        // Act - synchronize GitHub
        var syncResponse = await _client.PostAsync(
            $"/teams/{team.Id}/github/sync",
            null);

        syncResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        // Act - calculate Merged PRs
        var metricResponse = await _client.PostAsync(
            $"/teams/{team.Id}/metrics/merged-prs" +
            "?periodStart=2026-09-01" +
            "&periodEnd=2026-09-30",
            null);

        // Assert
        metricResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var metric = await metricResponse.Content
            .ReadFromJsonAsync<EngineeringMetricResponse>();

        metric.Should().NotBeNull();

        metric!.TeamId.Should()
            .Be(team.Id);

        metric.MetricType.Should()
            .Be(MetricType.MergedPRs);

        metric.Value.Should()
            .Be(2m);

        metric.PeriodStart.Should()
            .Be(new DateOnly(2026, 9, 1));

        metric.PeriodEnd.Should()
            .Be(new DateOnly(2026, 9, 30));
    }
}