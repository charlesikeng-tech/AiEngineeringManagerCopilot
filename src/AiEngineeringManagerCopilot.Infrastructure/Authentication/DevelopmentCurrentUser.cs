using AiEngineeringManagerCopilot.Application.Abstractions;

namespace AiEngineeringManagerCopilot.Infrastructure.Authentication;

public sealed class DevelopmentCurrentUser : ICurrentUser
{
    public Guid UserId { get; } =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
}