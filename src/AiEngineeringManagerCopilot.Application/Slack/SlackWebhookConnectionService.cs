using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiEngineeringManagerCopilot.Application.Slack;

public sealed class SlackWebhookConnectionService(
    ICurrentUser currentUser,
    ITeamRepository teamRepository,
    ISlackWebhookConnectionRepository connectionRepository,
    ISecretProtector secretProtector,
    ISlackWebhookClient slackWebhookClient,
    ReportNotificationFactory notificationFactory,
    ILogger<SlackWebhookConnectionService> logger)
    : ISlackWebhookConnectionService, IEngineeringReportNotifier
{
    public async Task<SlackWebhookConnectionResponse?> CreateAsync(
        Guid teamId,
        CreateSlackWebhookRequest request,
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

        if (!SlackWebhookAddress.TryParse(request.WebhookUrl, out var webhookUri))
        {
            throw new ArgumentException("Invalid Slack Incoming Webhook URL.", nameof(request));
        }

        if (await connectionRepository.GetByTeamIdAsync(teamId, cancellationToken) is not null)
        {
            throw new ConflictException("A Slack webhook already exists for this team.");
        }

        var connection = new SlackWebhookConnection
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            WebhookUrlEncrypted = secretProtector.Protect(webhookUri!.AbsoluteUri),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await connectionRepository.AddAsync(connection, cancellationToken);
        await connectionRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(connection);
    }

    public async Task<SlackWebhookConnectionResponse?> GetAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        if (await teamRepository.GetByIdAsync(teamId, currentUser.UserId, cancellationToken) is null)
        {
            return null;
        }

        var connection = await connectionRepository.GetByTeamIdAsync(teamId, cancellationToken);
        return connection is null ? null : ToResponse(connection);
    }

    public async Task<bool> DeleteAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        if (await teamRepository.GetByIdAsync(teamId, currentUser.UserId, cancellationToken) is null)
        {
            return false;
        }

        var connection = await connectionRepository.GetByTeamIdAsync(teamId, cancellationToken);
        if (connection is null)
        {
            return false;
        }

        await connectionRepository.DeleteAsync(connection, cancellationToken);
        await connectionRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TestSlackWebhookResponse?> TestAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        if (await teamRepository.GetByIdAsync(teamId, currentUser.UserId, cancellationToken) is null)
        {
            return null;
        }

        var connection = await connectionRepository.GetByTeamIdAsync(teamId, cancellationToken);
        if (connection is null)
        {
            return null;
        }

        bool sent;
        try
        {
            sent = await SendAsync(
                connection,
                "Slack webhook test successful.",
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Slack webhook test failed for team {TeamId}.", teamId);
            sent = false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Slack webhook test timed out for team {TeamId}.", teamId);
            sent = false;
        }

        return new TestSlackWebhookResponse(
            sent,
            sent ? "Slack webhook is valid." : "Slack webhook test failed.");
    }

    public async Task NotifyCreatedAsync(
        Guid teamId,
        string teamName,
        EngineeringReportResponse report,
        CancellationToken cancellationToken)
    {
        var connection = await connectionRepository.GetByTeamIdAsync(teamId, cancellationToken);
        if (connection is null)
        {
            return;
        }

        var notification = notificationFactory.Create(teamId, teamName, report);

        try
        {
            if (!await slackWebhookClient.SendReportAsync(GetWebhookUri(connection), notification, cancellationToken))
            {
                logger.LogWarning(
                    "Slack report notification was rejected for team {TeamId} and report {ReportId}.",
                    teamId,
                    report.Id);
            }
        }
        catch (HttpRequestException)
        {
            logger.LogWarning(
                "Slack report notification failed for team {TeamId} and report {ReportId}.",
                teamId,
                report.Id);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Slack report notification timed out for team {TeamId} and report {ReportId}.",
                teamId,
                report.Id);
        }
    }

    private async Task<bool> SendAsync(
        SlackWebhookConnection connection,
        string message,
        CancellationToken cancellationToken)
    {
        return await slackWebhookClient.SendAsync(GetWebhookUri(connection), message, cancellationToken);
    }

    private Uri GetWebhookUri(SlackWebhookConnection connection)
    {
        var url = secretProtector.Unprotect(connection.WebhookUrlEncrypted);
        if (!SlackWebhookAddress.TryParse(url, out var webhookUri))
        {
            throw new InvalidOperationException(
                $"Stored Slack webhook URL for team {connection.TeamId} is invalid.");
        }

        return webhookUri!;
    }

    private static SlackWebhookConnectionResponse ToResponse(
        SlackWebhookConnection connection) =>
        new(connection.TeamId, connection.CreatedAt);

}
