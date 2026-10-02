using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed class JiraSyncService(
    IJiraConnectionRepository jiraConnectionRepository,
    IJiraWorkItemRepository jiraWorkItemRepository,
    IJiraClient jiraClient,
    ISecretProtector secretProtector)
    : IJiraSyncService
{
    public async Task<JiraSyncResult> SyncAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var connection =
            await jiraConnectionRepository.GetByTeamIdAsync(
                teamId,
                cancellationToken);

        if (connection is null)
        {
            return new JiraSyncResult(
                Created: 0,
                Updated: 0,
                Total: 0);
        }

        var apiToken = secretProtector.Unprotect(
            connection.ApiTokenEncrypted);

        var issues = await jiraClient.GetIssuesAsync(
            connection.BaseUrl,
            connection.Email,
            apiToken,
            connection.ProjectKey,
            cancellationToken);

        var created = 0;
        var updated = 0;

        foreach (var issue in issues)
        {
            var existingWorkItem =
                await jiraWorkItemRepository.GetByExternalIdAsync(
                    teamId,
                    issue.Id,
                    cancellationToken);

            await jiraWorkItemRepository.UpsertAsync(
                new JiraWorkItem
                {
                    Id = existingWorkItem?.Id ?? Guid.NewGuid(),
                    TeamId = teamId,
                    ExternalId = issue.Id,
                    Key = issue.Key,
                    Summary = issue.Summary,
                    Status = issue.Status,
                    AssigneeExternalId =
                        issue.AssigneeAccountId,
                    CreatedAt = issue.CreatedAt,
                    DoneAt = issue.DoneAt,
                    IsBlocked = issue.IsBlocked
                },
                cancellationToken);

            if (existingWorkItem is null)
            {
                created++;
            }
            else
            {
                updated++;
            }
        }

        connection.LastSyncAt =
            DateTimeOffset.UtcNow;

        await jiraConnectionRepository.SaveChangesAsync(
            cancellationToken);

        return new JiraSyncResult(
            Created: created,
            Updated: updated,
            Total: issues.Count);
    }
}