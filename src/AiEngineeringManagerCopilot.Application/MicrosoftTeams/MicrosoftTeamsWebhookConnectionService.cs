using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiEngineeringManagerCopilot.Application.MicrosoftTeams;

public sealed class MicrosoftTeamsWebhookConnectionService(
    ICurrentUser currentUser,
    ITeamRepository teamRepository,
    IMicrosoftTeamsWebhookConnectionRepository connectionRepository,
    ISecretProtector secretProtector,
    IMicrosoftTeamsWebhookClient webhookClient,
    ReportNotificationFactory notificationFactory,
    ILogger<MicrosoftTeamsWebhookConnectionService> logger)
    : IMicrosoftTeamsWebhookConnectionService, IEngineeringReportNotifier
{
    public async Task<MicrosoftTeamsWebhookConnectionResponse?> CreateAsync(
        Guid teamId,
        CreateMicrosoftTeamsWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (await teamRepository.GetByIdAsync(teamId, currentUser.UserId, cancellationToken) is null)
        {
            return null;
        }

        if (!MicrosoftTeamsWebhookAddress.TryParse(request.WebhookUrl, out var webhookUri))
        {
            throw new ArgumentException("Invalid Microsoft Teams webhook URL.", nameof(request));
        }

        if (await connectionRepository.GetByTeamIdAsync(teamId, cancellationToken) is not null)
        {
            throw new ConflictException("A Microsoft Teams webhook already exists for this team.");
        }

        var connection = new MicrosoftTeamsWebhookConnection
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

    public async Task<MicrosoftTeamsWebhookConnectionResponse?> GetAsync(
        Guid teamId, CancellationToken cancellationToken)
    {
        if (await teamRepository.GetByIdAsync(teamId, currentUser.UserId, cancellationToken) is null)
        {
            return null;
        }

        var connection = await connectionRepository.GetByTeamIdAsync(teamId, cancellationToken);
        return connection is null ? null : ToResponse(connection);
    }

    public async Task<bool> DeleteAsync(Guid teamId, CancellationToken cancellationToken)
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

    public async Task<TestMicrosoftTeamsWebhookResponse?> TestAsync(
        Guid teamId, CancellationToken cancellationToken)
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

        var sent = await TrySendAsync(
            connection, "Microsoft Teams webhook test successful.", cancellationToken);
        return new TestMicrosoftTeamsWebhookResponse(
            sent, sent ? "Microsoft Teams webhook is valid." : "Microsoft Teams webhook test failed.");
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
        if (!await TrySendAsync(connection, notification.FallbackText, cancellationToken, notification))
        {
            logger.LogWarning(
                "Microsoft Teams report notification failed for team {TeamId} and report {ReportId}.",
                teamId, report.Id);
        }
    }

    private async Task<bool> TrySendAsync(
        MicrosoftTeamsWebhookConnection connection,
        string message,
        CancellationToken cancellationToken,
        ReportNotification? notification = null)
    {
        var url = secretProtector.Unprotect(connection.WebhookUrlEncrypted);
        if (!MicrosoftTeamsWebhookAddress.TryParse(url, out var webhookUri))
        {
            throw new InvalidOperationException(
                $"Stored Microsoft Teams webhook URL for team {connection.TeamId} is invalid.");
        }

        try
        {
            return notification is null
                ? await webhookClient.SendAsync(webhookUri!, message, cancellationToken)
                : await webhookClient.SendReportAsync(webhookUri!, notification, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Do not log the exception: HTTP errors can contain the signed webhook URL.
            logger.LogWarning("Microsoft Teams webhook delivery failed for team {TeamId}.",
                connection.TeamId);
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Microsoft Teams webhook delivery timed out for team {TeamId}.",
                connection.TeamId);
            return false;
        }
    }

    private static MicrosoftTeamsWebhookConnectionResponse ToResponse(
        MicrosoftTeamsWebhookConnection connection) => new(connection.TeamId, connection.CreatedAt);

}
