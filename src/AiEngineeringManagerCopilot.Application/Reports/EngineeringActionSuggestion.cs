using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringActionSuggestion(
    string Title,
    string Description,
    ActionPriority Priority);