namespace AiEngineeringManagerCopilot.Application.MicrosoftTeams;

public interface IMicrosoftTeamsWebhookConnectionService
{
    Task<MicrosoftTeamsWebhookConnectionResponse?> CreateAsync(
        Guid teamId, CreateMicrosoftTeamsWebhookRequest request, CancellationToken cancellationToken);

    Task<MicrosoftTeamsWebhookConnectionResponse?> GetAsync(
        Guid teamId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid teamId, CancellationToken cancellationToken);

    Task<TestMicrosoftTeamsWebhookResponse?> TestAsync(Guid teamId, CancellationToken cancellationToken);
}
