using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.UnitTests.AI;

public sealed class AIAnalysisEvidencePersistenceTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AIAnalysisEvidencePersistenceTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Evidence_ShouldBePersisted_WithAIAnalysis()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var analysis = new AIAnalysis
        {
            Id = Guid.NewGuid(),
            ReportId = Guid.NewGuid(),
            Summary = "Test analysis",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var evidence = new AIAnalysisEvidence
        {
            Id = Guid.NewGuid(),
            AIAnalysisId = analysis.Id,
            MetricType = "CycleTime",
            Value = 4.8m,
            Reason =
                "Cycle time increased compared with the previous period.",
            CreatedAt = DateTimeOffset.UtcNow
        };

        evidence.SetConfidence(0.92m);

        analysis.Evidence.Add(evidence);

        dbContext.AIAnalyses.Add(analysis);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var persisted = await dbContext.AIAnalyses
            .Include(x => x.Evidence)
            .SingleAsync(x => x.Id == analysis.Id);

        persisted.Evidence.Should().ContainSingle();

        var persistedEvidence =
            persisted.Evidence.Single();

        persistedEvidence.MetricType
            .Should().Be("CycleTime");

        persistedEvidence.Value
            .Should().Be(4.8m);

        persistedEvidence.Confidence
            .Should().Be(0.92m);

        persistedEvidence.Reason
            .Should().Be(
                "Cycle time increased compared with the previous period.");
    }
}