using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.IntegrationTests.Fakes;

public sealed class FakeGitHubClient : IGitHubClient
{
    
    public GitHubOwner? OwnerToReturn { get; set; }

    public string? ReceivedOwner { get; private set; }
    
    public GitHubOwnerType? ReceivedOwnerType { get; private set; }

    public string? ReceivedAccessToken { get; private set; }

    public List<GitHubRepository> Repositories { get; set; } = [];
    
    public Dictionary<string, List<GitHubPullRequest>>
        PullRequestsByRepository { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
    
    public Dictionary<string, Exception>
        PullRequestExceptionsByRepository { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
    
    public Dictionary<int, List<GitHubPullRequestReview>>
        ReviewsByPullRequestNumber { get; set; } = [];
    
    public Dictionary<int, Exception>
        ReviewExceptionsByPullRequestNumber { get; set; } = [];
    
    public Dictionary<string, List<GitHubDeployment>>
        DeploymentsByRepository { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<long, List<GitHubDeploymentStatus>>
        DeploymentStatusesByDeploymentId { get; set; } = [];
    
    public Dictionary<string, Exception>
        DeploymentExceptionsByRepository { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
    
    public Dictionary<long, Exception>
        DeploymentStatusExceptionsByDeploymentId { get; set; } = [];

    public Task<IReadOnlyList<GitHubRepository>> GetRepositoriesAsync(
        string owner,
        GitHubOwnerType ownerType,
        string accessToken,
        CancellationToken cancellationToken)
    {
        ReceivedOwner = owner;
        ReceivedAccessToken = accessToken;

        return Task.FromResult<IReadOnlyList<GitHubRepository>>(
            Repositories);
    }

    public Task<GitHubOwner?> GetOwnerAsync(
        string owner,
        GitHubOwnerType ownerType,
        string accessToken,
        CancellationToken cancellationToken)
    {
        ReceivedOwner = owner;
        ReceivedOwnerType = ownerType;
        ReceivedAccessToken = accessToken;
        
        return Task.FromResult(OwnerToReturn);
    }

    public Task<IReadOnlyList<GitHubPullRequest>> GetPullRequestsAsync(
        string accessToken,
        string owner,
        string repository,
        CancellationToken cancellationToken)
    {
        ReceivedOwner = owner;
        ReceivedAccessToken = accessToken;
        
        if (PullRequestExceptionsByRepository.TryGetValue(
                repository,
                out var exception))
        {
            return Task.FromException<
                IReadOnlyList<GitHubPullRequest>>(
                exception);
        }
        

        if (!PullRequestsByRepository.TryGetValue(
                repository,
                out var pullRequests))
        {
            return Task.FromResult<
                IReadOnlyList<GitHubPullRequest>>([]);
        }

        return Task.FromResult<
            IReadOnlyList<GitHubPullRequest>>(pullRequests);
    }

    public Task<IReadOnlyList<GitHubPullRequestReview>>
        GetPullRequestReviewsAsync(
            string accessToken,
            string owner,
            string repository,
            int pullRequestNumber,
            CancellationToken cancellationToken)
    {
        ReceivedOwner = owner;
        ReceivedAccessToken = accessToken;
        
        if (ReviewExceptionsByPullRequestNumber.TryGetValue(
                pullRequestNumber,
                out var exception))
        {
            return Task.FromException<
                IReadOnlyList<GitHubPullRequestReview>>(
                exception);
        }

        if (!ReviewsByPullRequestNumber.TryGetValue(
                pullRequestNumber,
                out var reviews))
        {
            return Task.FromResult<
                IReadOnlyList<GitHubPullRequestReview>>([]);
        }

        return Task.FromResult<
            IReadOnlyList<GitHubPullRequestReview>>(reviews);
    }

    public Task<IReadOnlyList<GitHubDeployment>>
        GetDeploymentsAsync(
            string accessToken,
            string owner,
            string repository,
            CancellationToken cancellationToken)
    {
        ReceivedOwner = owner;
        ReceivedAccessToken = accessToken;
        
        if (DeploymentExceptionsByRepository.TryGetValue(
                repository,
                out var exception))
        {
            return Task.FromException<
                IReadOnlyList<GitHubDeployment>>(
                exception);
        }
        
        if (!DeploymentsByRepository.TryGetValue(
                repository,
                out var deployments))
        {
            return Task.FromResult<
                IReadOnlyList<GitHubDeployment>>([]);
        }

        return Task.FromResult<
            IReadOnlyList<GitHubDeployment>>(deployments);
    }

    public Task<IReadOnlyList<GitHubDeploymentStatus>>
        GetDeploymentStatusesAsync(
            string accessToken,
            string owner,
            string repository,
            long deploymentId,
            CancellationToken cancellationToken)
    {
        ReceivedOwner = owner;
        ReceivedAccessToken = accessToken;

        if (DeploymentStatusExceptionsByDeploymentId.TryGetValue(
                deploymentId,
                out var exception))
        {
            return Task.FromException<
                IReadOnlyList<GitHubDeploymentStatus>>(exception);
        }
        
        if (!DeploymentStatusesByDeploymentId.TryGetValue(
                deploymentId,
                out var statuses))
        {
            return Task.FromResult<
                IReadOnlyList<GitHubDeploymentStatus>>([]);
        }

        return Task.FromResult<
            IReadOnlyList<GitHubDeploymentStatus>>(statuses);
    }
    
    public void Reset()
    {
        Repositories = [];

        PullRequestsByRepository.Clear();
        PullRequestExceptionsByRepository.Clear();

        ReviewsByPullRequestNumber.Clear();
        ReviewExceptionsByPullRequestNumber.Clear();

        DeploymentsByRepository.Clear();
        DeploymentExceptionsByRepository.Clear();

        DeploymentStatusesByDeploymentId.Clear();
        DeploymentStatusExceptionsByDeploymentId.Clear();

        OwnerToReturn = null;
        ReceivedOwner = null;
        ReceivedAccessToken = null;
    }
}