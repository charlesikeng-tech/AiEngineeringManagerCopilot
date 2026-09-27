using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Reports;

namespace AiEngineeringManagerCopilot.Application.Risks;

public sealed class EngineeringRiskService(
    ITeamRepository teamRepository,
    IEngineeringReportRepository reportRepository,
    IEngineeringRiskRepository riskRepository,
    ICurrentUser currentUser)
    : IEngineeringRiskService
{
    public async Task<EngineeringRisksResponse?> GetCurrentAsync(
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

        var risks = await riskRepository.GetByReportIdAsync(
            latestReport.Id,
            cancellationToken);

        return new EngineeringRisksResponse(
            teamId,
            latestReport.Id,
            latestReport.PeriodStart,
            latestReport.PeriodEnd,
            risks
                .Select(risk => new EngineeringRiskResponse(
                    risk.Id,
                    risk.ReportId,
                    risk.MetricType,
                    risk.Severity,
                    risk.Category,
                    risk.Title,
                    risk.Description,
                    risk.Recommendation,
                    risk.CreatedAt))
                .ToList());
    }
}