using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class DeploymentRepository(
    AppDbContext dbContext)
    : IDeploymentRepository
{
    public async Task AddAsync(
        Deployment deployment,
        CancellationToken cancellationToken)
    {
        await dbContext.Deployments.AddAsync(
            deployment,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Deployment>>
        GetByTeamAndPeriodAsync(
            Guid teamId,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(
            periodStart.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var end = new DateTimeOffset(
            periodEnd
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        return await dbContext.Deployments
            .Join(
                dbContext.Repositories,
                deployment => deployment.RepositoryId,
                repository => repository.Id,
                (deployment, repository) => new
                {
                    Deployment = deployment,
                    repository.TeamId
                })
            .Where(x =>
                x.TeamId == teamId &&
                x.Deployment.DeployedAt >= start &&
                x.Deployment.DeployedAt < end)
            .Select(x => x.Deployment)
            .OrderBy(x => x.DeployedAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(
            cancellationToken);
    }
}