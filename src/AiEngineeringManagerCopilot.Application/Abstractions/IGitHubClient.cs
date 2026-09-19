using AiEngineeringManagerCopilot.Application.GitHub;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IGitHubClient
{
    Task<GitHubOrganization?> GetOrganizationAsync(
        string organization,
        string accessToken,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GitHubRepository>> GetRepositoriesAsync(
        string organization,
        string accessToken,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<GitHubPullRequest>> GetPullRequestsAsync(
        string accessToken,
        string owner,
        string repository,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<GitHubPullRequestReview>>
        GetPullRequestReviewsAsync(
            string accessToken,
            string owner,
            string repository,
            int pullRequestNumber,
            CancellationToken cancellationToken);
    
    Task<IReadOnlyList<GitHubDeployment>> GetDeploymentsAsync(
        string accessToken,
        string owner,
        string repository,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<GitHubDeploymentStatus>>
        GetDeploymentStatusesAsync(
            string accessToken,
            string owner,
            string repository,
            long deploymentId,
            CancellationToken cancellationToken);
}

public sealed record GitHubOrganization(
    long Id,
    string Login,
    string Name,
    string HtmlUrl);

public sealed record GitHubRepository(
    long Id,
    string Name,
    string FullName,
    string HtmlUrl,
    string? DefaultBranch);