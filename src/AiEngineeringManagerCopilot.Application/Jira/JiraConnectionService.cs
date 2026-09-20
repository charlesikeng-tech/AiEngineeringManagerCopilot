using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed class JiraConnectionService(
    ICurrentUser currentUser,
    ITeamRepository teamRepository,
    IJiraConnectionRepository jiraConnectionRepository,
    ISecretProtector secretProtector,
    IJiraClient jiraClient)
    : IJiraConnectionService
{
    public async Task<JiraConnectionResponse?> CreateAsync(
        Guid teamId,
        CreateJiraConnectionRequest request,
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
            await jiraConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        if (existingConnection is not null)
        {
            throw new ConflictException(
                "A Jira connection already exists for this team.");
        }

        var baseUrl = request.BaseUrl.Trim().TrimEnd('/');
        var email = request.Email.Trim();
        var apiToken = request.ApiToken.Trim();
        
        var projectKey = request.ProjectKey
            .Trim()
            .ToUpperInvariant();

        var encryptedToken =
            secretProtector.Protect(apiToken);

        var connection = new JiraConnection
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            BaseUrl = baseUrl,
            Email = email,
            ApiTokenEncrypted = encryptedToken,
            ProjectKey = projectKey,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await jiraConnectionRepository.AddAsync(
            connection,
            cancellationToken);

        await jiraConnectionRepository.SaveChangesAsync(
            cancellationToken);

        return JiraConnectionResponse.FromEntity(
            connection);
    }

    public async Task<JiraConnectionResponse?> GetAsync(
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
            await jiraConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        return connection is null
            ? null
            : JiraConnectionResponse.FromEntity(connection);
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
            await jiraConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        if (connection is null)
        {
            return false;
        }

        await jiraConnectionRepository.DeleteAsync(
            connection,
            cancellationToken);

        await jiraConnectionRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<TestJiraConnectionResponse?> TestAsync(
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
            await jiraConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        if (connection is null)
        {
            return null;
        }

        var apiToken = secretProtector.Unprotect(
            connection.ApiTokenEncrypted);

        var jiraUser = await jiraClient.GetCurrentUserAsync(
            connection.BaseUrl,
            connection.Email,
            apiToken,
            cancellationToken);

        if (jiraUser is null)
        {
            return new TestJiraConnectionResponse(
                false,
                null,
                "Jira connection is not valid.");
        }

        return new TestJiraConnectionResponse(
            true,
            jiraUser.DisplayName,
            "Jira connection is valid.");
    }
}