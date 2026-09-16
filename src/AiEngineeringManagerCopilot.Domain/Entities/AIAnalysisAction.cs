using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class AIAnalysisAction
{
    public Guid Id { get; set; }

    public Guid AIAnalysisId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ActionPriority Priority { get; set; }
}