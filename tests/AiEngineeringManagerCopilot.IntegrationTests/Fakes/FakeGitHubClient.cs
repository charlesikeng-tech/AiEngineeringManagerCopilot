using AiEngineeringManagerCopilot.Application.Abstractions;

namespace AiEngineeringManagerCopilot.IntegrationTests.Fakes;

public sealed class FakeGitHubClient : IGitHubClient
{
    public bool ShouldReturnOrganization { get; set; } = true;

    public string? ReceivedOrganization { get; private set; }

    public string? ReceivedAccessToken { get; private set; }
    
    public List<GitHubRepository> Repositories { get; set; } = [];

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
}