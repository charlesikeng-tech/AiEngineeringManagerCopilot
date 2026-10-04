namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string? Email { get; set; }

    public bool EmailVerified { get; set; }

    public bool IsActive { get; set; } = true;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}