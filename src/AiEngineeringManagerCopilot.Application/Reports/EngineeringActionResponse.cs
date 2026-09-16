using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public sealed record EngineeringActionResponse(
    Guid Id,
    Guid ReportId,
    string Title,
    string Description,
    ActionPriority Priority,
    string? Owner,
    DateOnly? DueDate,
    string Status,
    DateTimeOffset CreatedAt);