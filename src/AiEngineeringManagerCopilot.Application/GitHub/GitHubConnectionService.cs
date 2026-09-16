using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed class GitHubConnectionService(
    ICurrentUser currentUser,
    ITeamRepository teamRepository,
    IGitHubConnectionRepository gitHubConnectionRepository,
    ISecretProtector secretProtector,
    IGitHubClient gitHubClient)
    : IGitHubConnectionService
{
    public async Task<GitHubConnectionResponse?> CreateAsync(
        Guid teamId,
        CreateGitHubConnectionRequest request,
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

        var existingConnection =
            await gitHubConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        if (existingConnection is not null)
        {
            throw new ConflictException(
                "A GitHub connection already exists for this team.");
        }

        var organization = request.Organization.Trim();
        var accessToken = request.AccessToken.Trim();

        var encryptedToken = secretProtector.Protect(
            accessToken);

        var connection = new GitHubConnection
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Organization = organization,
            AccessTokenEncrypted = encryptedToken,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await gitHubConnectionRepository.AddAsync(
            connection,
            cancellationToken);

        await gitHubConnectionRepository.SaveChangesAsync(
            cancellationToken);

        return GitHubConnectionResponse.FromEntity(
            connection);
    }

    public async Task<GitHubConnectionResponse?> GetAsync(
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

        return connection is null
            ? null
            : GitHubConnectionResponse.FromEntity(connection);
    }

    public async Task<bool> DeleteAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return false;
        }

        var connection =
            await gitHubConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        if (connection is null)
        {
            return false;
        }

        await gitHubConnectionRepository.DeleteAsync(
            connection,
            cancellationToken);

        await gitHubConnectionRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
    
    public async Task<TestGitHubConnectionResponse?> TestAsync(
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

        var organization =
            await gitHubClient.GetOrganizationAsync(
                connection.Organization,
                accessToken,
                cancellationToken);

        if (organization is null)
        {
            return new TestGitHubConnectionResponse(
                false,
                connection.Organization,
                "GitHub organization was not found or is not accessible.");
        }

        return new TestGitHubConnectionResponse(
            true,
            organization.Login,
            "GitHub connection is valid.");
    }
}