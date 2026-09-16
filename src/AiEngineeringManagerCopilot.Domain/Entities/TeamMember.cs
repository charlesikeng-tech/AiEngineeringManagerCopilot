using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class TeamMember
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public TeamMemberRole Role { get; set; }

    public string? ProviderUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}