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
            var httpContext = httpContextAccessor.HttpContext
                              ?? throw new InvalidOperationException(
                                  "No active HTTP context is available.");

            var user = httpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                throw new InvalidOperationException(
                    "The current user is not authenticated.");
            }

            var value = user.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(value, out var userId) ||
                userId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The authenticated user does not have a valid user identifier.");
            }

            return userId;
        }
    }
}