using AiEngineeringManagerCopilot.Application.Authentication;
using FluentAssertions;
using System.Security.Claims;

namespace AiEngineeringManagerCopilot.UnitTests.Authentication;

public sealed class CurrentUserTests
{
    [Fact]
    public void UserId_ShouldReturnAuthenticatedUserId()
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

        var currentUser = new CurrentUser(principal);

        // Act
        var result = currentUser.UserId;

        // Assert
        result.Should().Be(userId);
    }
    
    [Fact]
    public void UserId_ShouldReturnEmpty_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity());

        var currentUser = new CurrentUser(principal);

        // Act
        var result = currentUser.UserId;

        // Assert
        result.Should().Be(Guid.Empty);
    }
}