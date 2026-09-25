using System.Security.Claims;
using AiEngineeringManagerCopilot.Application.Abstractions;

namespace AiEngineeringManagerCopilot.Application.Authentication;

public sealed class CurrentUser(
    ClaimsPrincipal principal)
    : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = principal
                .FindFirst(ClaimTypes.NameIdentifier)
                ?.Value;

            return Guid.TryParse(value, out var userId)
                ? userId
                : Guid.Empty;
        }
    }
}