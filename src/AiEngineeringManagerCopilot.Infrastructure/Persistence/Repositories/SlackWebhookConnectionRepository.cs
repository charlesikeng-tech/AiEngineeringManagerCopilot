using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class SlackWebhookConnectionRepository(AppDbContext dbContext)
    : ISlackWebhookConnectionRepository
{
    public Task<SlackWebhookConnection?> GetByTeamIdAsync(
        Guid teamId,
        CancellationToken cancellationToken) =>
        dbContext.SlackWebhookConnections.FirstOrDefaultAsync(
            connection => connection.TeamId == teamId,
            cancellationToken);

    public async Task AddAsync(
        SlackWebhookConnection connection,
        CancellationToken cancellationToken) =>
        await dbContext.SlackWebhookConnections.AddAsync(connection, cancellationToken);

    public Task DeleteAsync(
        SlackWebhookConnection connection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.SlackWebhookConnections.Remove(connection);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
