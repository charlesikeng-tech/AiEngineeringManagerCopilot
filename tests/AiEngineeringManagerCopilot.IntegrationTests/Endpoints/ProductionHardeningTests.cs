using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.AI;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Helpers;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiEngineeringManagerCopilot.IntegrationTests.Endpoints;

public sealed class ProductionHardeningTests
{
    [Fact]
    public async Task ReportAndAnalysisUseFrozenMetrics_AndConcurrentAnalysisCallsLlmOnce()
    {
        var provider = new CountingProvider();
        using var factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILlmProvider>();
                services.AddSingleton<ILlmProvider>(provider);
            }));
        using var client = factory.CreateClient();
        var teamResponse = await client.PostAsJsonAsync("/teams", new { Name = $"Snapshot {Guid.NewGuid():N}" });
        teamResponse.EnsureSuccessStatusCode();
        var team = (await teamResponse.Content.ReadApiJsonAsync<TeamResponse>())!;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.EngineeringMetrics.Add(new EngineeringMetric
            {
                Id = Guid.NewGuid(), TeamId = team.Id, MetricType = MetricType.CycleTime,
                Value = 12m, DataStatus = MetricDataStatus.Available,
                PeriodStart = new DateOnly(2026, 9, 1), PeriodEnd = new DateOnly(2026, 9, 30),
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
        var generated = await client.PostAsync($"/teams/{team.Id}/reports?periodStart=2026-09-01&periodEnd=2026-09-30", null);
        generated.EnsureSuccessStatusCode();
        var report = (await generated.Content.ReadApiJsonAsync<EngineeringReportResponse>())!;
        Assert.True(report.HasSnapshot);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var metric = await db.EngineeringMetrics.SingleAsync(x => x.TeamId == team.Id);
            metric.Value = 500m;
            await db.SaveChangesAsync();
        }
        var reread = await client.GetAsync($"/teams/{team.Id}/reports/{report.Id}");
        reread.EnsureSuccessStatusCode();
        var frozen = (await reread.Content.ReadApiJsonAsync<EngineeringReportResponse>())!;
        Assert.Equal(12m, frozen.Metrics.Single().Value);
        Assert.Equal(report.OverallScore, frozen.OverallScore);
        var url = $"/teams/{team.Id}/reports/{report.Id}/analyze";
        var responses = await Task.WhenAll(client.PostAsync(url, null), client.PostAsync(url, null));
        foreach (var response in responses) response.EnsureSuccessStatusCode();
        Assert.Equal(1, provider.Calls);
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await verifyDb.AIAnalyses.CountAsync(x => x.ReportId == report.Id));
        var evidence = await verifyDb.AIAnalysisEvidence.SingleAsync(x => x.AIAnalysisId ==
            verifyDb.AIAnalyses.Where(a => a.ReportId == report.Id).Select(a => a.Id).Single());
        Assert.Equal(12m, evidence.Value);
    }

    [Fact]
    public async Task AnalysisQuotaReturns429_WhileReadsRemainAvailable()
    {
        using var factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.UseSetting("Testing:AIAnalysisPermitLimit", "1"));
        using var client = factory.CreateClient();
        var url = $"/teams/{Guid.NewGuid()}/reports/{Guid.NewGuid()}/analyze";
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync(url, null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync(url, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url.Replace("/analyze", "/analysis"))).StatusCode);
    }

    private sealed class CountingProvider : ILlmProvider
    {
        private int calls;
        public int Calls => Volatile.Read(ref calls);
        public async Task<LlmAnalysisResult> AnalyzeAsync(string prompt, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(150, cancellationToken);
            return await new FakeLlmProvider().AnalyzeAsync(prompt, cancellationToken);
        }
    }
}
