using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Reports;

namespace AiEngineeringManagerCopilot.Application.Actions;

public sealed class EngineeringActionService(
    ITeamRepository teamRepository,
    IEngineeringReportRepository reportRepository,
    IEngineeringActionRepository actionRepository,
    ICurrentUser currentUser)
    : IEngineeringActionService
{
    public async Task<EngineeringActionsResponse?> GetCurrentAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var reports = await reportRepository.GetByTeamAsync(
            teamId,
            cancellationToken);

        var latestReport = reports
            .OrderByDescending(x => x.PeriodEnd)
            .ThenByDescending(x => x.PeriodStart)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (latestReport is null)
        {
            return null;
        }

        var actions = await actionRepository.GetByReportIdAsync(
            latestReport.Id,
            cancellationToken);

        return new EngineeringActionsResponse(
            teamId,
            latestReport.Id,
            latestReport.PeriodStart,
            latestReport.PeriodEnd,
            actions
                .Select(action => new EngineeringActionResponse(
                    action.Id,
                    action.ReportId,
                    action.MetricType?.ToString(),
                    action.Title,
                    action.Description,
                    action.Priority,
                    action.Owner,
                    action.DueDate,
                    action.Status.ToString(),
                    action.CreatedAt))
                .ToList());
    }

    public async Task<EngineeringActionResponse?> UpdateAsync(
        Guid teamId,
        Guid actionId,
        UpdateEngineeringActionRequest request,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(
            teamId,
            currentUser.UserId,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var reports = await reportRepository.GetByTeamAsync(
            teamId,
            cancellationToken);

        var reportIds = reports
            .Select(x => x.Id)
            .ToHashSet();

        var action = await actionRepository.GetByIdAsync(
            actionId,
            cancellationToken);

        if (action is null ||
            !reportIds.Contains(action.ReportId))
        {
            return null;
        }

        if (request.Status.HasValue)
        {
            action.Status = request.Status.Value;
        }

        if (request.Owner is not null)
        {
            action.Owner = request.Owner.Trim();
        }

        if (request.DueDate.HasValue)
        {
            action.DueDate = request.DueDate.Value;
        }

        await actionRepository.SaveChangesAsync(
            cancellationToken);

        return new EngineeringActionResponse(
            action.Id,
            action.ReportId,
            action.MetricType?.ToString(),
            action.Title,
            action.Description,
            action.Priority,
            action.Owner,
            action.DueDate,
            action.Status.ToString(),
            action.CreatedAt);
    }
}