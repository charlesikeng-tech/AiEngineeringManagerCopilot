using AiEngineeringManagerCopilot.Application.Abstractions;

namespace AiEngineeringManagerCopilot.Infrastructure.Authentication;

public sealed class DevelopmentBootstrapCurrentUser : ICurrentUser
{
    public Guid UserId =>
        Guid.Parse("11111111-1111-1111-1111-111111111111");
}