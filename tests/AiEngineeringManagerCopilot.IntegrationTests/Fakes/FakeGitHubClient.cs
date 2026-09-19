using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;

namespace AiEngineeringManagerCopilot.IntegrationTests.Fakes;

public sealed class FakeGitHubClient : IGitHubClient
{
    public bool ShouldReturnOrganization { get; set; } = true;

    public string? ReceivedOrganization { get; private set; }

    public string? ReceivedAccessToken { get; private set; }

    public List<GitHubRepository> Repositories { get; set; } = [];
    
    public Dictionary<string, List<GitHubPullRequest>>
        PullRequestsByRepository { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
    
    public Dictionary<int, List<GitHubPullRequestReview>>
        ReviewsByPullRequestNumber { get; set; } = [];
    
    public Dictionary<string, List<GitHubDeployment>>
        DeploymentsByRepository { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<long, List<GitHubDeploymentStatus>>
        DeploymentStatusesByDeploymentId { get; set; } = [];

    public Task<GitHubOrganization?> GetOrganizationAsync(
        string organization,
        string accessToken,
        CancellationToken cancellationToken)
    {
        ReceivedOrganization = organization;
        ReceivedAccessToken = accessToken;

        if (!ShouldReturnOrganization)
        {
            return Task.FromResult<GitHubOrganization?>(null);
        }

        return Task.FromResult<GitHubOrganization?>(
            new GitHubOrganization(
                123456,
                organization,
                "Test Organization",
                $"https://github.com/{organization}"));
    }

    public Task<IReadOnlyList<GitHubRepository>> GetRepositoriesAsync(
        string organization,
        string accessToken,
        CancellationToken cancellationToken)
    {
        ReceivedOrganization = organization;
        ReceivedAccessToken = accessToken;

        return Task.FromResult<IReadOnlyList<GitHubRepository>>(
            Repositories);
    }

    public Task<IReadOnlyList<GitHubPullRequest>> GetPullRequestsAsync(
        string accessToken,
        string owner,
        string repository,
        CancellationToken cancellationToken)
    {
        ReceivedOrganization = owner;
        ReceivedAccessToken = accessToken;

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
        ReceivedOrganization = owner;
        ReceivedAccessToken = accessToken;

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
        ReceivedOrganization = owner;
        ReceivedAccessToken = accessToken;

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
        ReceivedOrganization = owner;
        ReceivedAccessToken = accessToken;

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
        ReviewsByPullRequestNumber.Clear();

        DeploymentsByRepository.Clear();
        DeploymentStatusesByDeploymentId.Clear();

        ReceivedOrganization = null;
        ReceivedAccessToken = null;
    }
}