namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringInsight(
    string Category,
    string Title,
    string Description,
    string Impact,
    string Recommendation);