using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed class GitHubSyncService(
    ICurrentUser currentUser,
    ITeamRepository teamRepository,
    IGitHubConnectionRepository gitHubConnectionRepository,
    IRepositoryRepository repositoryRepository,
    ISecretProtector secretProtector,
    IGitHubClient gitHubClient)
    : IGitHubSyncService
{
    public async Task<GitHubSyncResponse?> SyncAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

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

        await gitHubConnectionRepository.SaveChangesAsync(
            cancellationToken);

        return new GitHubSyncResponse(
            repositories.Count,
            created,
            updated);
    }
}