using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface IMicrosoftTeamsWebhookConnectionRepository
{
    Task<MicrosoftTeamsWebhookConnection?> GetByTeamIdAsync(
        Guid teamId, CancellationToken cancellationToken);

    Task AddAsync(MicrosoftTeamsWebhookConnection connection, CancellationToken cancellationToken);

    Task DeleteAsync(MicrosoftTeamsWebhookConnection connection, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
