using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Actions;

public sealed record UpdateEngineeringActionRequest(
    ActionStatus? Status,
    string? Owner,
    DateOnly? DueDate);