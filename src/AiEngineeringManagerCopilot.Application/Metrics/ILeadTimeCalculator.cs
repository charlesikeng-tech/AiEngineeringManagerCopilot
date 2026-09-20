using AiEngineeringManagerCopilot.Domain.Entities;

namespace AiEngineeringManagerCopilot.Application.Metrics;

public interface ILeadTimeCalculator
{
    LeadTimeResult Calculate(
        IReadOnlyCollection<JiraWorkItem> workItems,
        DateOnly periodStart,
        DateOnly periodEnd);
}