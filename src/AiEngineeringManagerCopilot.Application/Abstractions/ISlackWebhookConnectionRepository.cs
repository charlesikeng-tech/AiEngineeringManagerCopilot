using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Abstractions;

public interface ISlackWebhookConnectionRepository
{
    Task<SlackWebhookConnection?> GetByTeamIdAsync(
        Guid teamId,
        CancellationToken cancellationToken);

    Task AddAsync(
        SlackWebhookConnection connection,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        SlackWebhookConnection connection,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
