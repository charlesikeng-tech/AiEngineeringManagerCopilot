using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class MicrosoftTeamsWebhookConnectionRepository(AppDbContext dbContext)
    : IMicrosoftTeamsWebhookConnectionRepository
{
    public Task<MicrosoftTeamsWebhookConnection?> GetByTeamIdAsync(
        Guid teamId, CancellationToken cancellationToken) =>
        dbContext.MicrosoftTeamsWebhookConnections.FirstOrDefaultAsync(
            connection => connection.TeamId == teamId, cancellationToken);

    public async Task AddAsync(
        MicrosoftTeamsWebhookConnection connection, CancellationToken cancellationToken) =>
        await dbContext.MicrosoftTeamsWebhookConnections.AddAsync(connection, cancellationToken);

    public Task DeleteAsync(
        MicrosoftTeamsWebhookConnection connection, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.MicrosoftTeamsWebhookConnections.Remove(connection);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
