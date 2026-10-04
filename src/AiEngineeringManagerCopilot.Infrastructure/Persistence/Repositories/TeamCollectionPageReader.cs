using AiEngineeringManagerCopilot.Application.Actions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Application.TeamMembers;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Repositories;

public sealed class TeamCollectionPageReader(AppDbContext db) : ITeamCollectionPageReader
{
    private Task<bool> OwnsTeamAsync(Guid ownerId, Guid teamId, CancellationToken ct) =>
        db.Teams.AnyAsync(team => team.Id == teamId && team.OwnerUserId == ownerId, ct);

    private Task<EngineeringReport?> LatestReportAsync(Guid ownerId, Guid teamId, CancellationToken ct) =>
        OrderedReports(ownerId, teamId).FirstOrDefaultAsync(ct);

    private IOrderedQueryable<EngineeringReport> OrderedReports(Guid ownerId, Guid teamId) =>
        db.EngineeringReports.AsNoTracking()
            .Where(report => report.TeamId == teamId &&
                db.Teams.Any(team => team.Id == report.TeamId && team.OwnerUserId == ownerId))
            .OrderByDescending(report => report.PeriodEnd)
            .ThenByDescending(report => report.PeriodStart)
            .ThenByDescending(report => report.CreatedAt)
            .ThenByDescending(report => report.Id);

    public async Task<PagedResult<ReportHistoryItem>?> GetReportsAsync(
        Guid ownerId, Guid teamId, PageRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsTeamAsync(ownerId, teamId, cancellationToken)) return null;
        var query = OrderedReports(ownerId, teamId);
        var total = await query.CountAsync(cancellationToken);
        // The extra older row supplies deltas for the final displayed row across page boundaries.
        var reports = await query.Skip(request.Offset).Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);
        var items = reports.Take(request.PageSize).Select((report, index) =>
        {
            var older = index + 1 < reports.Count ? reports[index + 1] : null;
            return new ReportHistoryItem(
                report.Id, report.TeamId, report.PeriodStart, report.PeriodEnd,
                report.OverallScore,
                EngineeringHealthLevelResolver.Resolve(report.OverallScore, report.DataCoverage),
                report.DataCoverage, report.CreatedAt,
                older is null ? null : report.OverallScore - older.OverallScore,
                older is null ? null : report.DataCoverage - older.DataCoverage);
        }).ToArray();
        return new(items, total, request.PageNumber, request.PageSize);
    }

    public async Task<PagedResult<TeamMemberResponse>?> GetMembersAsync(
        Guid ownerId, Guid teamId, PageRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsTeamAsync(ownerId, teamId, cancellationToken)) return null;
        var query = db.TeamMembers.AsNoTracking().Where(member => member.TeamId == teamId);
        var total = await query.CountAsync(cancellationToken);
        var members = await query.OrderBy(member => member.Name).ThenBy(member => member.Id)
            .Skip(request.Offset).Take(request.PageSize).ToListAsync(cancellationToken);
        return new(members.Select(TeamMemberResponse.FromEntity).ToArray(),
            total, request.PageNumber, request.PageSize);
    }

    public async Task<EngineeringRisksPageResponse?> GetRisksAsync(
        Guid ownerId, Guid teamId, PageRequest request, CancellationToken cancellationToken)
    {
        var report = await LatestReportAsync(ownerId, teamId, cancellationToken);
        if (report is null) return null;
        var query = db.EngineeringRisks.AsNoTracking()
            .Where(risk => risk.ReportId == report.Id && risk.TeamId == teamId);
        var total = await query.CountAsync(cancellationToken);
        var counts = await query.GroupBy(risk => risk.Severity)
            .Select(group => new { Severity = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        int Count(RiskSeverity severity) => counts.SingleOrDefault(item => item.Severity == severity)?.Count ?? 0;
        var risks = await query.OrderBy(risk =>
                risk.Severity == RiskSeverity.Critical ? 0 :
                risk.Severity == RiskSeverity.High ? 1 :
                risk.Severity == RiskSeverity.Medium ? 2 : 3)
            .ThenByDescending(risk => risk.CreatedAt).ThenBy(risk => risk.Id)
            .Skip(request.Offset).Take(request.PageSize).ToListAsync(cancellationToken);
        var items = risks.Select(risk => new EngineeringRiskResponse(
            risk.Id, risk.ReportId, risk.MetricType, risk.Severity, risk.Category,
            risk.Title, risk.Description, risk.Recommendation, risk.CreatedAt)).ToArray();
        return new(teamId, report.Id, report.PeriodStart, report.PeriodEnd,
            new(items, total, request.PageNumber, request.PageSize),
            new(Count(RiskSeverity.Critical), Count(RiskSeverity.High),
                Count(RiskSeverity.Medium), Count(RiskSeverity.Low)));
    }

    public async Task<EngineeringActionsPageResponse?> GetActionsAsync(
        Guid ownerId, Guid teamId, PageRequest request, CancellationToken cancellationToken)
    {
        var report = await LatestReportAsync(ownerId, teamId, cancellationToken);
        if (report is null) return null;
        var query = db.EngineeringActions.AsNoTracking().Where(action => action.ReportId == report.Id);
        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var total = await query.CountAsync(cancellationToken);
        var counts = await query.GroupBy(action => action.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        int Count(ActionStatus status) => counts.SingleOrDefault(item => item.Status == status)?.Count ?? 0;
        var overdue = await query.CountAsync(action =>
            action.Status != ActionStatus.Done && action.Status != ActionStatus.Cancelled &&
            action.DueDate < asOfDate, cancellationToken);
        var actions = await query.OrderBy(action =>
                action.Priority == ActionPriority.Critical ? 0 :
                action.Priority == ActionPriority.High ? 1 :
                action.Priority == ActionPriority.Medium ? 2 : 3)
            .ThenBy(action => action.Status == ActionStatus.InProgress ? 0 :
                action.Status == ActionStatus.Todo ? 1 :
                action.Status == ActionStatus.Done ? 2 : 3)
            .ThenByDescending(action => action.CreatedAt).ThenBy(action => action.Id)
            .Skip(request.Offset).Take(request.PageSize).ToListAsync(cancellationToken);
        var items = actions.Select(action => new EngineeringActionResponse(
            action.Id, action.ReportId, action.MetricType?.ToString(), action.Title,
            action.Description, action.Priority, action.Owner, action.DueDate,
            action.Status.ToString(), action.CreatedAt)).ToArray();
        return new(teamId, report.Id, report.PeriodStart, report.PeriodEnd,
            new(items, total, request.PageNumber, request.PageSize),
            new(Count(ActionStatus.Todo), Count(ActionStatus.InProgress),
                Count(ActionStatus.Done), Count(ActionStatus.Cancelled), overdue, asOfDate));
    }
}
