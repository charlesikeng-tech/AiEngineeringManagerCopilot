using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using AiEngineeringManagerCopilot.Application.Actions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Application.TeamMembers;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;
using AiEngineeringManagerCopilot.IntegrationTests.Helpers;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Endpoints;

public sealed class TeamCollectionPaginationEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("reports")]
    [InlineData("members")]
    [InlineData("risks")]
    [InlineData("actions")]
    public async Task GetPage_ShouldHideForeignAndMissingTeamsIncludingCounts(string collection)
    {
        var team = await SeedAsync(foreign: true);
        foreach (var teamId in new[] { team.Id, Guid.NewGuid() })
        {
            var response = await _client.GetAsync($"/teams/{teamId}/{collection}/paged");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("reports")]
    [InlineData("members")]
    [InlineData("risks")]
    [InlineData("actions")]
    public async Task GetPage_ShouldRejectInvalidOrMalformedBounds(string collection)
    {
        foreach (var query in new[]
        {
            "pageNumber=0", "pageNumber=-1", "pageSize=0", "pageSize=-1", "pageSize=101",
            "pageNumber=2147483647&pageSize=100", "pageNumber=oops", "pageSize=oops",
            "pageNumber=2147483648"
        })
        {
            var response = await _client.GetAsync($"/teams/{Guid.NewGuid()}/{collection}/paged?{query}");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, query);
        }
    }

    [Fact]
    public async Task GetPage_ShouldUseDefaultsAndHandleOwnedEmptyCollections()
    {
        var team = await SeedAsync(withReport: false);
        var reports = await GetAsync<PagedResult<ReportHistoryItem>>(team.Id, "reports");
        var members = await GetAsync<PagedResult<TeamMemberResponse>>(team.Id, "members");
        reports.Should().BeEquivalentTo(new PagedResult<ReportHistoryItem>([], 0, 1, 10));
        members.Should().BeEquivalentTo(new PagedResult<TeamMemberResponse>([], 0, 1, 10));
        foreach (var collection in new[] { "risks", "actions" })
            (await _client.GetAsync($"/teams/{team.Id}/{collection}/paged"))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);

        var withReport = await SeedAsync();
        var risks = await GetAsync<EngineeringRisksPageResponse>(withReport.Id, "risks");
        var actions = await GetAsync<EngineeringActionsPageResponse>(withReport.Id, "actions");
        risks.Page.Items.Should().BeEmpty();
        risks.Page.TotalCount.Should().Be(0);
        risks.Page.PageSize.Should().Be(10);
        risks.Summary.Should().Be(new RiskPageSummary(0, 0, 0, 0));
        actions.Page.Items.Should().BeEmpty();
        actions.Page.TotalCount.Should().Be(0);
        actions.Summary.Should().Be(new ActionPageSummary(
            0, 0, 0, 0, 0, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public async Task Reports_ShouldOrderTiesAndComputeDeltasAcrossPageBoundaries()
    {
        var team = await SeedAsync(withReport: false);
        var reports = new[]
        {
            Report(team.Id, 80, 90, end: new(2026, 9, 30)),
            Report(team.Id, 70, 80, end: new(2026, 9, 30)),
            Report(team.Id, 50, 60, end: new(2026, 8, 31)),
            Report(team.Id, 0, 0, end: new(2026, 7, 31))
        };
        await SaveAsync(db => db.EngineeringReports.AddRange(reports));
        var ordered = reports.OrderByDescending(report => report.PeriodEnd)
            .ThenByDescending(report => report.PeriodStart)
            .ThenByDescending(report => report.CreatedAt)
            .ThenByDescending(report => report.Id).ToArray();
        var first = await GetAsync<PagedResult<ReportHistoryItem>>(team.Id, "reports", "pageSize=2");
        var last = await GetAsync<PagedResult<ReportHistoryItem>>(team.Id, "reports", "pageNumber=2&pageSize=2");
        first.TotalCount.Should().Be(4);
        first.Items.Concat(last.Items).Select(report => report.Id).Should().Equal(ordered.Select(report => report.Id));
        var all = first.Items.Concat(last.Items).ToArray();
        for (var i = 0; i < ordered.Length - 1; i++)
        {
            all[i].ScoreDelta.Should().Be(ordered[i].OverallScore - ordered[i + 1].OverallScore);
            all[i].CoverageDelta.Should().Be(ordered[i].DataCoverage - ordered[i + 1].DataCoverage);
        }
        all[^1].ScoreDelta.Should().BeNull();
        all[^1].CoverageDelta.Should().BeNull();
        all[^1].HealthLevel.Should().Be("No Data");
        all[2].HealthLevel.Should().Be("At Risk");
        var beyond = await GetAsync<PagedResult<ReportHistoryItem>>(team.Id, "reports", "pageNumber=3&pageSize=2");
        beyond.Items.Should().BeEmpty();
        beyond.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task Members_ShouldOrderDuplicateNamesByIdAndReturnLastAndEmptyPages()
    {
        var team = await SeedAsync(withReport: false);
        var members = new[] { Member(team.Id, "Z"), Member(team.Id, "A"), Member(team.Id, "A") };
        await SaveAsync(db => db.TeamMembers.AddRange(members));
        var first = await GetAsync<PagedResult<TeamMemberResponse>>(team.Id, "members", "pageSize=2");
        var last = await GetAsync<PagedResult<TeamMemberResponse>>(team.Id, "members", "pageNumber=2&pageSize=2");
        first.TotalCount.Should().Be(3);
        first.Items.Concat(last.Items).Select(member => member.Id)
            .Should().Equal(members.OrderBy(member => member.Name).ThenBy(member => member.Id).Select(member => member.Id));
        last.Items.Should().ContainSingle();
        var beyond = await GetAsync<PagedResult<TeamMemberResponse>>(team.Id, "members", "pageNumber=3&pageSize=2");
        beyond.Items.Should().BeEmpty();
        beyond.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Risks_ShouldUseEntireLatestReportSummaryAndSeverityOrdering()
    {
        var team = await SeedAsync(withReport: false);
        var reports = LatestSelectionReports(team.Id);
        var latest = reports[^1];
        var risks = new[]
        {
            Risk(team.Id, latest.Id, RiskSeverity.Low),
            Risk(team.Id, latest.Id, RiskSeverity.Medium),
            Risk(team.Id, latest.Id, RiskSeverity.High),
            Risk(team.Id, latest.Id, RiskSeverity.Critical),
            Risk(team.Id, latest.Id, RiskSeverity.Critical),
            Risk(team.Id, latest.Id, RiskSeverity.Critical)
        };
        risks[^1].CreatedAt = CreatedAt.AddDays(1);
        await SaveAsync(db =>
        {
            db.EngineeringReports.AddRange(reports);
            db.EngineeringRisks.AddRange(risks);
            db.EngineeringRisks.Add(Risk(team.Id, reports[0].Id, RiskSeverity.Low));
        });
        var pages = new List<EngineeringRisksPageResponse>();
        for (var page = 1; page <= 4; page++)
            pages.Add(await GetAsync<EngineeringRisksPageResponse>(
                team.Id, "risks", $"pageNumber={page}&pageSize=2"));
        pages.Should().OnlyContain(page => page.ReportId == latest.Id && page.Page.TotalCount == 6);
        pages.Select(page => page.Summary).Should().OnlyContain(summary => summary == new RiskPageSummary(3, 1, 1, 1));
        pages.SelectMany(page => page.Page.Items).Select(risk => risk.Id).Should().Equal(
            risks.OrderByDescending(risk => risk.Severity).ThenByDescending(risk => risk.CreatedAt)
                .ThenBy(risk => risk.Id).Select(risk => risk.Id));
        pages[^1].Page.Items.Should().BeEmpty();
        var raw = await _client.GetStringAsync($"/teams/{team.Id}/risks/paged");
        raw.Should().Contain("\"severity\":\"Critical\"");
    }

    [Fact]
    public async Task Actions_ShouldUseGlobalSummaryUtcOverdueAndStablePriorityStatusOrdering()
    {
        var team = await SeedAsync(withReport: false);
        var reports = LatestSelectionReports(team.Id);
        var latest = reports[^1];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var actions = new[]
        {
            Action(latest.Id, ActionPriority.Low, ActionStatus.Todo, today.AddDays(-1)),
            Action(latest.Id, ActionPriority.Medium, ActionStatus.InProgress, today),
            Action(latest.Id, ActionPriority.High, ActionStatus.Todo, null),
            Action(latest.Id, ActionPriority.Critical, ActionStatus.Cancelled, today.AddDays(-1)),
            Action(latest.Id, ActionPriority.Critical, ActionStatus.Done, today.AddDays(-1)),
            Action(latest.Id, ActionPriority.Critical, ActionStatus.Todo, today.AddDays(1)),
            Action(latest.Id, ActionPriority.Critical, ActionStatus.InProgress, today.AddDays(-1)),
            Action(latest.Id, ActionPriority.Critical, ActionStatus.InProgress, null),
            Action(latest.Id, ActionPriority.Critical, ActionStatus.InProgress, null)
        };
        actions[^1].CreatedAt = CreatedAt.AddDays(1);
        await SaveAsync(db =>
        {
            db.EngineeringReports.AddRange(reports);
            db.EngineeringActions.AddRange(actions);
            db.EngineeringActions.Add(Action(reports[0].Id, ActionPriority.Critical, ActionStatus.Todo, today.AddDays(-1)));
        });
        var pages = new List<EngineeringActionsPageResponse>();
        for (var page = 1; page <= 6; page++)
            pages.Add(await GetAsync<EngineeringActionsPageResponse>(
                team.Id, "actions", $"pageNumber={page}&pageSize=2"));
        pages.Should().OnlyContain(page => page.ReportId == latest.Id && page.Page.TotalCount == 9);
        pages.Select(page => page.Summary)
            .Should().OnlyContain(summary => summary == new ActionPageSummary(3, 4, 1, 1, 2, today));
        pages.SelectMany(page => page.Page.Items).Select(action => action.Id).Should().Equal(
            actions.OrderByDescending(action => action.Priority)
                .ThenBy(action => action.Status == ActionStatus.InProgress ? 0 : (int)action.Status)
                .ThenByDescending(action => action.CreatedAt).ThenBy(action => action.Id).Select(action => action.Id));
        pages[^1].Page.Items.Should().BeEmpty();
        var raw = await _client.GetStringAsync($"/teams/{team.Id}/actions/paged");
        raw.Should().Contain("\"priority\":\"Critical\"").And.Contain("\"status\":\"InProgress\"")
            .And.Contain($"\"asOfDate\":\"{today:yyyy-MM-dd}\"");

        var update = await _client.PatchAsJsonAsync($"/teams/{team.Id}/actions/{actions[6].Id}",
            new UpdateEngineeringActionRequest(ActionStatus.Done, "Owner", today.AddDays(-1)));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var reloaded = await GetAsync<EngineeringActionsPageResponse>(team.Id, "actions", "pageSize=2");
        reloaded.Summary.Should().Be(new ActionPageSummary(3, 3, 2, 1, 1, today));
        reloaded.Page.Items.Should().NotContain(action => action.Id == actions[6].Id);
    }

    [Theory]
    [InlineData("reports")]
    [InlineData("members")]
    [InlineData("risks")]
    [InlineData("actions")]
    public async Task Reader_ShouldExecuteSqlCountAndBoundedStablePageQueries(string collection)
    {
        var team = await SeedAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var existingDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var recorder = new QueryRecorder();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(existingDb.Database.GetConnectionString()).AddInterceptors(recorder).Options;
        await using var db = new AppDbContext(options);
        var reader = new TeamCollectionPageReader(db);
        var request = new PageRequest(2, 2);
        switch (collection)
        {
            case "reports": await reader.GetReportsAsync(OwnerId, team.Id, request, default); break;
            case "members": await reader.GetMembersAsync(OwnerId, team.Id, request, default); break;
            case "risks": await reader.GetRisksAsync(OwnerId, team.Id, request, default); break;
            case "actions": await reader.GetActionsAsync(OwnerId, team.Id, request, default); break;
        }
        recorder.Commands.Should().Contain(command => command.Sql.Contains("count(*)"));
        var paged = recorder.Commands.Where(command => command.Sql.Contains("OFFSET")).Should().ContainSingle().Subject;
        paged.Sql.Should().Contain("ORDER BY").And.Contain("LIMIT").And.Contain("\"Id\"");
        paged.Parameters.Should().Contain(2).And.Contain(collection == "reports" ? 3 : 2);
        // All materialized collection reads must be bounded; aggregate reads only return grouped counts.
        recorder.Commands.Where(command =>
                !command.Sql.Contains("count(*)") && !command.Sql.Contains("EXISTS"))
            .Should().OnlyContain(command => command.Sql.Contains("LIMIT"));
        if (collection is "risks" or "actions")
        {
            var latest = recorder.Commands.Where(command =>
                command.Sql.Contains("engineering_reports") && command.Sql.Contains("LIMIT 1"))
                .Should().ContainSingle().Subject;
            latest.Sql.Should().Contain("\"PeriodEnd\" DESC").And.Contain("\"PeriodStart\" DESC")
                .And.Contain("\"CreatedAt\" DESC").And.Contain("\"Id\" DESC");
        }
    }

    private async Task<T> GetAsync<T>(Guid teamId, string collection, string query = "")
    {
        var response = await _client.GetAsync($"/teams/{teamId}/{collection}/paged?{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadApiJsonAsync<T>())!;
    }

    private async Task<Team> SeedAsync(bool foreign = false, bool withReport = true)
    {
        var owner = foreign ? Guid.NewGuid() : OwnerId;
        var team = new Team { Id = Guid.NewGuid(), Name = "Pagination", OwnerUserId = owner, CreatedAt = CreatedAt };
        await SaveAsync(db =>
        {
            if (foreign)
                db.Users.Add(new User { Id = owner, Email = $"{owner}@example.com", Name = "Other", CreatedAt = CreatedAt });
            db.Teams.Add(team);
            if (withReport)
            {
                var report = Report(team.Id);
                db.EngineeringReports.Add(report);
                if (foreign)
                {
                    db.TeamMembers.Add(Member(team.Id, "Private"));
                    db.EngineeringRisks.Add(Risk(team.Id, report.Id, RiskSeverity.Critical));
                    db.EngineeringActions.Add(Action(report.Id, ActionPriority.Critical, ActionStatus.Todo, null));
                }
            }
        });
        return team;
    }

    private async Task SaveAsync(Action<AppDbContext> seed)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }

    private static EngineeringReport Report(Guid teamId, int score = 80, decimal coverage = 90,
        DateOnly? start = null, DateOnly? end = null) => new()
    {
        Id = Guid.NewGuid(), TeamId = teamId, OverallScore = score, DataCoverage = coverage,
        PeriodStart = start ?? new(2026, 7, 1), PeriodEnd = end ?? new(2026, 9, 30), CreatedAt = CreatedAt
    };

    private static EngineeringReport[] LatestSelectionReports(Guid teamId)
    {
        var reports = new[]
        {
            Report(teamId, end: new(2026, 8, 31)),
            Report(teamId, start: new(2026, 8, 1)),
            Report(teamId, start: new(2026, 9, 1)),
            Report(teamId, start: new(2026, 9, 1)),
            Report(teamId, start: new(2026, 9, 1))
        };
        reports[0].CreatedAt = CreatedAt.AddYears(1);
        reports[3].CreatedAt = reports[4].CreatedAt = CreatedAt.AddDays(1);
        var tiedIds = new[] { reports[3].Id, reports[4].Id }.Order().ToArray();
        reports[3].Id = tiedIds[0];
        reports[4].Id = tiedIds[1];
        return reports;
    }

    private static TeamMember Member(Guid teamId, string name) => new()
    {
        Id = Guid.NewGuid(), TeamId = teamId, Name = name,
        Email = $"{Guid.NewGuid():N}@example.com", CreatedAt = CreatedAt, Role = TeamMemberRole.Developer
    };

    private static EngineeringRisk Risk(Guid teamId, Guid reportId, RiskSeverity severity) => new()
    {
        Id = Guid.NewGuid(), TeamId = teamId, ReportId = reportId, Severity = severity,
        Title = "Risk", Description = "Description", Recommendation = "Recommendation", CreatedAt = CreatedAt
    };

    private static EngineeringAction Action(Guid reportId, ActionPriority priority, ActionStatus status, DateOnly? due) => new()
    {
        Id = Guid.NewGuid(), ReportId = reportId, Priority = priority, Status = status,
        Title = "Action", Description = "Description", CreatedAt = CreatedAt, DueDate = due
    };

    private sealed class QueryRecorder : DbCommandInterceptor
    {
        public List<(string Sql, object?[] Parameters)> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add((command.CommandText,
                command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value).ToArray()));
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
