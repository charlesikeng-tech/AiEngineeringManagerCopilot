using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Health;

public sealed class EngineeringHealthScoreServiceTests
{
    [Fact]
    public async Task CalculateAsync_ShouldCalculateScoreFromAllMetrics()
    {
        var teamId = Guid.NewGuid();

        var repository = new FakeEngineeringMetricRepository();

        repository.AddMetric(
            teamId,
            MetricType.CycleTime,
            8);

        repository.AddMetric(
            teamId,
            MetricType.PRReviewTime,
            4);

        repository.AddMetric(
            teamId,
            MetricType.DeploymentFrequency,
            20);

        repository.AddMetric(
            teamId,
            MetricType.ChangeFailureRate,
            5);

        repository.AddMetric(
            teamId,
            MetricType.LeadTime,
            24);

        repository.AddMetric(
            teamId,
            MetricType.OpenPRs,
            2);

        repository.AddMetric(
            teamId,
            MetricType.MergedPRs,
            20);

        repository.AddMetric(
            teamId,
            MetricType.BlockedItems,
            0);

        var calculator =
            new EngineeringHealthScoreCalculator();

        var service =
            new EngineeringHealthScoreService(
                new FakeTeamRepository(teamId),
                new FakeCurrentUser(),
                repository,
                calculator);

        var result = await service.CalculateAsync(
            teamId,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.TeamId.Should().Be(teamId);
        result.OverallScore.Should().Be(100);
        result.HealthLevel.Should().Be("Excellent");
    }

    private sealed class FakeEngineeringMetricRepository
        : IEngineeringMetricRepository
    {
        private readonly List<EngineeringMetric> _metrics = [];

        public void AddMetric(
            Guid teamId,
            MetricType metricType,
            decimal value)
        {
            _metrics.Add(
                new EngineeringMetric
                {
                    Id = Guid.NewGuid(),
                    TeamId = teamId,
                    MetricType = metricType,
                    Value = value,
                    PeriodStart = new DateOnly(2026, 9, 1),
                    PeriodEnd = new DateOnly(2026, 9, 30),
                    CreatedAt = DateTimeOffset.UtcNow
                });
        }

        public Task AddAsync(
            EngineeringMetric metric,
            CancellationToken cancellationToken)
        {
            _metrics.Add(metric);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EngineeringMetric>>
            GetByTeamAndPeriodAsync(
                Guid teamId,
                MetricType metricType,
                DateOnly periodStart,
                DateOnly periodEnd,
                CancellationToken cancellationToken)
        {
            IReadOnlyList<EngineeringMetric> result =
                _metrics
                    .Where(x =>
                        x.TeamId == teamId &&
                        x.MetricType == metricType &&
                        x.PeriodStart >= periodStart &&
                        x.PeriodEnd <= periodEnd)
                    .ToList();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<EngineeringMetric>> GetLatestByTeamAsync(
            Guid teamId, 
            CancellationToken cancellationToken)
        {
            IReadOnlyList<EngineeringMetric> result =
                _metrics
                    .Where(x =>
                        x.TeamId == teamId)
                    .GroupBy(x => x.MetricType)
                    .Select(group => group
                        .OrderByDescending(x => x.PeriodEnd)
                        .ThenByDescending(x => x.CreatedAt)
                        .First())
                    .OrderBy(x => x.MetricType)
                    .ToList();
            
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<EngineeringMetric>> GetByTeamAndPeriodAsync(
            Guid teamId, 
            DateOnly periodStart, 
            DateOnly periodEnd,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<EngineeringMetric> result = 
                _metrics
                    .Where(x =>
                        x.TeamId == teamId &&
                        x.PeriodStart == periodStart &&
                        x.PeriodEnd == periodEnd)
                    .ToList();
            
            return Task.FromResult(result);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
    
    private sealed class FakeTeamRepository : ITeamRepository
    {
        private readonly Guid _teamId;

        public FakeTeamRepository(Guid teamId)
        {
            _teamId = teamId;
        }

        public Task<Team?> GetByIdAsync(
            Guid teamId,
            Guid ownerUserId,
            CancellationToken cancellationToken)
        {
            Team? team = teamId == _teamId
                ? new Team
                {
                    Id = _teamId,
                    OwnerUserId = ownerUserId,
                    Name = "Test Team",
                    CreatedAt = DateTimeOffset.UtcNow
                }
                : null;

            return Task.FromResult(team);
        }

        public Task<IReadOnlyList<Team>> GetByOwnerAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Team> teams = [];

            return Task.FromResult(teams);
        }

        public Task AddAsync(
            Team team,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SaveChangesAsync(
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DeleteAsync(
            Team team,
            CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId { get; } =
            Guid.Parse("11111111-1111-1111-1111-111111111111");
    }
}