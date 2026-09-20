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
            var workItem =
                await jiraWorkItemRepository.GetByExternalIdAsync(
                    teamId,
                    issue.Id,
                    cancellationToken);

            if (workItem is null)
            {
                workItem = new JiraWorkItem
                {
                    Id = Guid.NewGuid(),
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
                };

                await jiraWorkItemRepository.AddAsync(
                    workItem,
                    cancellationToken);

                created++;

                continue;
            }

            workItem.Key = issue.Key;
            workItem.Summary = issue.Summary;
            workItem.Status = issue.Status;
            workItem.AssigneeExternalId =
                issue.AssigneeAccountId;
            workItem.CreatedAt = issue.CreatedAt;
            workItem.DoneAt = issue.DoneAt;
            workItem.IsBlocked = issue.IsBlocked;

            updated++;
        }

        await jiraWorkItemRepository.SaveChangesAsync(
            cancellationToken);

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