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
                connection.Owner,
                connection.OwnerType,
                accessToken,
                cancellationToken);

        var created = 0;
        var updated = 0;

        foreach (var githubRepository in repositories)
        {
            var existingRepository =
                await repositoryRepository.GetByExternalIdAsync(
                    teamId,
                    githubRepository.Id,
                    cancellationToken);

            var repository = new Repository
            {
                Id = existingRepository?.Id ?? Guid.NewGuid(),
                TeamId = teamId,
                ExternalId = githubRepository.Id,
                Name = githubRepository.Name,
                FullName = githubRepository.FullName,
                Url = githubRepository.HtmlUrl,
                DefaultBranch = githubRepository.DefaultBranch,
                IsActive = true
            };

            await repositoryRepository.UpsertAsync(
                repository,
                cancellationToken);

            if (existingRepository is null)
            {
                created++;
            }
            else
            {
                updated++;
            }
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
                        connection.Owner,
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
                    await pullRequestRepository.UpsertAsync(
                        new PullRequest
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
                        },
                        cancellationToken);
                
                IReadOnlyList<GitHubPullRequestReview> reviews;

                try
                {
                    reviews =
                        await gitHubClient.GetPullRequestReviewsAsync(
                            accessToken,
                            connection.Owner,
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
                    await pullRequestReviewRepository.UpsertAsync(
                        new PullRequestReview
                        {
                            Id = Guid.NewGuid(),
                            ExternalId = githubReview.Id,
                            PullRequestId = pullRequest.Id,
                            ReviewerExternalId =
                                githubReview.ReviewerExternalId,
                            SubmittedAt = githubReview.SubmittedAt,
                            State = MapPullRequestReviewState(
                                githubReview.State)
                        },
                        cancellationToken);
                }
            }

            // Deployments belong to the repository,
            // not to a Pull Request.
            IReadOnlyList<GitHubDeployment> deployments;

            try
            {
                deployments =
                    await gitHubClient.GetDeploymentsAsync(
                        accessToken,
                        connection.Owner,
                        repository.Name,
                        cancellationToken);
            }
            catch (HttpRequestException)
            {
                deployments = [];
            }

            foreach (var githubDeployment in deployments)
            {
                IReadOnlyList<GitHubDeploymentStatus> statuses;

                try
                {
                    statuses =
                        await gitHubClient.GetDeploymentStatusesAsync(
                            accessToken,
                            connection.Owner,
                            repository.Name,
                            githubDeployment.Id,
                            cancellationToken);
                }
                catch (HttpRequestException)
                {
                    statuses = [];
                }

                var latestStatus = statuses
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                await deploymentRepository.UpsertAsync(
                    new Deployment
                    {
                        Id = Guid.NewGuid(),
                        RepositoryId = repository.Id,
                        ExternalId = githubDeployment.Id,
                        Environment = githubDeployment.Environment,
                        Status = latestStatus?.State ?? "unknown",
                        DeployedAt =
                            latestStatus?.CreatedAt ??
                            githubDeployment.CreatedAt
                    },
                    cancellationToken);
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