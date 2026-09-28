using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringActionSuggestion(
    MetricType? MetricType,
    string Title,
    string Description,
    ActionPriority Priority);