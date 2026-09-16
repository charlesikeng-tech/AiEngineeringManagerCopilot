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