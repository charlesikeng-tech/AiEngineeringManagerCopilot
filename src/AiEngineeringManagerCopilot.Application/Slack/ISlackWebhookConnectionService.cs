namespace AiEngineeringManagerCopilot.Application.Slack;

public interface ISlackWebhookConnectionService
{
    Task<SlackWebhookConnectionResponse?> CreateAsync(
        Guid teamId,
        CreateSlackWebhookRequest request,
        CancellationToken cancellationToken);

    Task<SlackWebhookConnectionResponse?> GetAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task<TestSlackWebhookResponse?> TestAsync(
        Guid teamId,
        CancellationToken cancellationToken);
}
