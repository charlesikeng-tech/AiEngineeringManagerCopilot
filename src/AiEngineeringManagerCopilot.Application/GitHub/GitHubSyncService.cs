using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed class GitHubSyncService(
    IGitHubConnectionRepository gitHubConnectionRepository,
    IRepositoryRepository repositoryRepository,
    IPullRequestRepository pullRequestRepository,
    IPullRequestReviewRepository pullRequestReviewRepository,
    IDeploymentRepository deploymentRepository,
    ISecretProtector secretProtector,
    IGitHubClient gitHubClient)
    : IGitHubSyncService
{
    public async Task<GitHubSyncResponse?> SyncAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        
        var connection =
            await gitHubConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        if (connection is null)
        {
            return null;
        }

        var accessToken = secretProtector.Unprotect(
            connection.AccessTokenEncrypted);

        var repositories =
            await gitHubClient.GetRepositoriesAsync(
                connection.Organization,
                accessToken,
                cancellationToken);

        var created = 0;
        var updated = 0;

        foreach (var githubRepository in repositories)
        {
            var repository =
                await repositoryRepository.GetByExternalIdAsync(
                    teamId,
                    githubRepository.Id,
                    cancellationToken);

            if (repository is null)
            {
                repository = new Repository
                {
                    Id = Guid.NewGuid(),
                    TeamId = teamId,
                    ExternalId = githubRepository.Id,
                    Name = githubRepository.Name,
                    FullName = githubRepository.FullName,
                    Url = githubRepository.HtmlUrl,
                    DefaultBranch = githubRepository.DefaultBranch,
                    IsActive = true
                };

                await repositoryRepository.AddAsync(
                    repository,
                    cancellationToken);

                created++;

                continue;
            }

            repository.Name = githubRepository.Name;
            repository.FullName = githubRepository.FullName;
            repository.Url = githubRepository.HtmlUrl;
            repository.DefaultBranch =
                githubRepository.DefaultBranch;
            repository.IsActive = true;

            updated++;
        }

        connection.LastSyncAt = DateTimeOffset.UtcNow;

        await repositoryRepository.SaveChangesAsync(
            cancellationToken);

        var teamRepositories =
            await repositoryRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        foreach (var repository in teamRepositories)
        {
            IReadOnlyList<GitHubPullRequest> pullRequests;

            try
            {
                pullRequests =
                    await gitHubClient.GetPullRequestsAsync(
                        accessToken,
                        connection.Organization,
                        repository.Name,
                        cancellationToken);
            }
            catch (HttpRequestException)
            {
                continue;
            }

            foreach (var githubPullRequest in pullRequests)
            {
                var pullRequest =
                    await pullRequestRepository.GetByExternalIdAsync(
                        repository.Id,
                        githubPullRequest.Id,
                        cancellationToken);

                if (pullRequest is null)
                {
                    pullRequest = new PullRequest
                    {
                        Id = Guid.NewGuid(),
                        RepositoryId = repository.Id,
                        ExternalId = githubPullRequest.Id,
                        AuthorExternalId =
                            githubPullRequest.AuthorExternalId,
                        Title = githubPullRequest.Title,
                        State = MapPullRequestState(
                            githubPullRequest),
                        CreatedAt = githubPullRequest.CreatedAt,
                        MergedAt = githubPullRequest.MergedAt,
                        ClosedAt = githubPullRequest.ClosedAt,
                        IsBlocked = false
                    };

                    await pullRequestRepository.AddAsync(
                        pullRequest,
                        cancellationToken);
                }
                else
                {
                    pullRequest.AuthorExternalId =
                        githubPullRequest.AuthorExternalId;

                    pullRequest.Title =
                        githubPullRequest.Title;

                    pullRequest.State =
                        MapPullRequestState(githubPullRequest);

                    pullRequest.MergedAt =
                        githubPullRequest.MergedAt;

                    pullRequest.ClosedAt =
                        githubPullRequest.ClosedAt;
                }

                IReadOnlyList<GitHubPullRequestReview> reviews;

                try
                {
                    reviews =
                        await gitHubClient.GetPullRequestReviewsAsync(
                            accessToken,
                            connection.Organization,
                            repository.Name,
                            githubPullRequest.Number,
                            cancellationToken);
                }
                catch (HttpRequestException)
                {
                    reviews = [];
                }

                foreach (var githubReview in reviews)
                {
                    var review =
                        await pullRequestReviewRepository
                            .GetByExternalIdAsync(
                                pullRequest.Id,
                                githubReview.Id,
                                cancellationToken);

                    if (review is null)
                    {
                        review = new PullRequestReview
                        {
                            Id = Guid.NewGuid(),
                            ExternalId = githubReview.Id,
                            PullRequestId = pullRequest.Id,
                            ReviewerExternalId =
                                githubReview.ReviewerExternalId,
                            SubmittedAt =
                                githubReview.SubmittedAt,
                            State = MapPullRequestReviewState(
                                githubReview.State)
                        };

                        await pullRequestReviewRepository.AddAsync(
                            review,
                            cancellationToken);

                        continue;
                    }

                    review.ReviewerExternalId =
                        githubReview.ReviewerExternalId;

                    review.SubmittedAt =
                        githubReview.SubmittedAt;

                    review.State =
                        MapPullRequestReviewState(
                            githubReview.State);
                }
            }

            // Deployments belong to the repository,
            // not to a Pull Request.
            IReadOnlyList<GitHubDeployment> githubDeployments;

            try
            {
                githubDeployments =
                    await gitHubClient.GetDeploymentsAsync(
                        accessToken,
                        connection.Organization,
                        repository.Name,
                        cancellationToken);
            }
            catch (HttpRequestException)
            {
                githubDeployments = [];
            }

            foreach (var githubDeployment in githubDeployments)
            {
                IReadOnlyList<GitHubDeploymentStatus> statuses;

                try
                {
                    statuses =
                        await gitHubClient.GetDeploymentStatusesAsync(
                            accessToken,
                            connection.Organization,
                            repository.Name,
                            githubDeployment.Id,
                            cancellationToken);
                }
                catch (HttpRequestException)
                {
                    continue;
                }

                var latestStatus = statuses
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                if (latestStatus is null)
                {
                    continue;
                }

                var deployment =
                    await deploymentRepository.GetByExternalIdAsync(
                        repository.Id,
                        githubDeployment.Id,
                        cancellationToken);

                if (deployment is null)
                {
                    deployment = new Deployment
                    {
                        Id = Guid.NewGuid(),
                        RepositoryId = repository.Id,
                        ExternalId = githubDeployment.Id,
                        Environment = githubDeployment.Environment,
                        Status = latestStatus.State,
                        DeployedAt = githubDeployment.CreatedAt
                    };

                    await deploymentRepository.AddAsync(
                        deployment,
                        cancellationToken);

                    continue;
                }

                deployment.Environment =
                    githubDeployment.Environment;

                deployment.Status =
                    latestStatus.State;

                deployment.DeployedAt =
                    githubDeployment.CreatedAt;
            }
        }

        await pullRequestRepository.SaveChangesAsync(
            cancellationToken);

        await pullRequestReviewRepository.SaveChangesAsync(
            cancellationToken);

        await deploymentRepository.SaveChangesAsync(
            cancellationToken);

        await gitHubConnectionRepository.SaveChangesAsync(
            cancellationToken);

        return new GitHubSyncResponse(
            repositories.Count,
            created,
            updated);
    }

    private static PullRequestState MapPullRequestState(
        GitHubPullRequest pullRequest)
    {
        if (pullRequest.MergedAt.HasValue)
        {
            return PullRequestState.Merged;
        }

        if (string.Equals(
                pullRequest.State,
                "closed",
                StringComparison.OrdinalIgnoreCase))
        {
            return PullRequestState.Closed;
        }

        return PullRequestState.Open;
    }

    private static PullRequestReviewState MapPullRequestReviewState(
        string state)
    {
        return state.ToUpperInvariant() switch
        {
            "APPROVED" =>
                PullRequestReviewState.Approved,

            "CHANGES_REQUESTED" =>
                PullRequestReviewState.ChangesRequested,

            "COMMENTED" =>
                PullRequestReviewState.Commented,

            "DISMISSED" =>
                PullRequestReviewState.Dismissed,

            "PENDING" =>
                PullRequestReviewState.Pending,

            _ =>
                PullRequestReviewState.Pending
        };
    }
}