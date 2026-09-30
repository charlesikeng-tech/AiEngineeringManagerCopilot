using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IGitHubClient
{
    Task<GitHubOwner?> GetOwnerAsync(
        string owner,
        GitHubOwnerType ownerType,
        string accessToken,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GitHubRepository>> GetRepositoriesAsync(
        string owner,
        GitHubOwnerType ownerType,
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

public sealed record GitHubOwner(
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