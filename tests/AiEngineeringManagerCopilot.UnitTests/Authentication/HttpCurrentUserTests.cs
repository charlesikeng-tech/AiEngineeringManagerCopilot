using System.Security.Claims;
using AiEngineeringManagerCopilot.Api.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace AiEngineeringManagerCopilot.UnitTests.Authentication;

public sealed class HttpCurrentUserTests
{
    [Fact]
    public void UserId_ShouldReturnUserIdFromHttpContext()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        userId.ToString())
                ],
                authenticationType: "Test"));

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = httpContext
            };

        var currentUser =
            new HttpCurrentUser(httpContextAccessor);

        // Act
        var result = currentUser.UserId;

        // Assert
        result.Should().Be(userId);
    }
    
    [Fact]
    public void UserId_ShouldReturnEmpty_WhenHttpContextIsMissing()
    {
        // Arrange
        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = null
            };

        var currentUser =
            new HttpCurrentUser(httpContextAccessor);

        // Act
        var result = currentUser.UserId;

        // Assert
        result.Should().Be(Guid.Empty);
    }
}