using System.Security.Claims;
using AiEngineeringManagerCopilot.Application.Abstractions;

namespace AiEngineeringManagerCopilot.Api.Authentication;

public sealed class HttpCurrentUser(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)
                ?.Value;

            return Guid.TryParse(value, out var userId)
                ? userId
                : Guid.Empty;
        }
    }
}