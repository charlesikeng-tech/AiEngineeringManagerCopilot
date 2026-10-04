using AiEngineeringManagerCopilot.Application.Actions;
using AiEngineeringManagerCopilot.Application.Risks;
using AiEngineeringManagerCopilot.Application.TeamMembers;

namespace AiEngineeringManagerCopilot.Application.Common;

public sealed record ReportHistoryItem(
    Guid Id, Guid TeamId, DateOnly PeriodStart, DateOnly PeriodEnd,
    int OverallScore, string HealthLevel, decimal DataCoverage, DateTimeOffset CreatedAt,
    int? ScoreDelta, decimal? CoverageDelta);

public sealed record RiskPageSummary(int CriticalCount, int HighCount, int MediumCount, int LowCount);

public sealed record ActionPageSummary(
    int TodoCount, int InProgressCount, int DoneCount, int CancelledCount,
    int OverdueCount, DateOnly AsOfDate);

public sealed record EngineeringRisksPageResponse(
    Guid TeamId, Guid ReportId, DateOnly PeriodStart, DateOnly PeriodEnd,
    PagedResult<EngineeringRiskResponse> Page, RiskPageSummary Summary);

public sealed record EngineeringActionsPageResponse(
    Guid TeamId, Guid ReportId, DateOnly PeriodStart, DateOnly PeriodEnd,
    PagedResult<EngineeringActionResponse> Page, ActionPageSummary Summary);

public interface ITeamCollectionPageReader
{
    Task<PagedResult<ReportHistoryItem>?> GetReportsAsync(
        Guid ownerId, Guid teamId, PageRequest request, CancellationToken cancellationToken);
    Task<PagedResult<TeamMemberResponse>?> GetMembersAsync(
        Guid ownerId, Guid teamId, PageRequest request, CancellationToken cancellationToken);
    Task<EngineeringRisksPageResponse?> GetRisksAsync(
        Guid ownerId, Guid teamId, PageRequest request, CancellationToken cancellationToken);
    Task<EngineeringActionsPageResponse?> GetActionsAsync(
        Guid ownerId, Guid teamId, PageRequest request, CancellationToken cancellationToken);
}
