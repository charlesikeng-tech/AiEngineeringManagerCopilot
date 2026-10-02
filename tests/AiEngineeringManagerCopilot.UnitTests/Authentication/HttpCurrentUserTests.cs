using System.Security.Claims;
using AiEngineeringManagerCopilot.Api.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace AiEngineeringManagerCopilot.UnitTests.Authentication;

public sealed class HttpCurrentUserTests
{
    [Fact]
    public void UserId_ShouldReturnUserId_WhenAuthenticatedUserHasValidIdentifier()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var principal = CreateAuthenticatedPrincipal(
            userId.ToString());

        var currentUser = CreateCurrentUser(principal);

        // Act
        var result = currentUser.UserId;

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void UserId_ShouldThrow_WhenHttpContextIsMissing()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = null
        };

        var currentUser =
            new HttpCurrentUser(httpContextAccessor);

        // Act
        var act = () => currentUser.UserId;

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("No active HTTP context is available.");
    }

    [Fact]
    public void UserId_ShouldThrow_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity());

        var currentUser = CreateCurrentUser(principal);

        // Act
        var act = () => currentUser.UserId;

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("The current user is not authenticated.");
    }

    [Fact]
    public void UserId_ShouldThrow_WhenNameIdentifierIsInvalid()
    {
        // Arrange
        var principal = CreateAuthenticatedPrincipal(
            "not-a-guid");

        var currentUser = CreateCurrentUser(principal);

        // Act
        var act = () => currentUser.UserId;

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "The authenticated user does not have a valid user identifier.");
    }

    [Fact]
    public void UserId_ShouldThrow_WhenNameIdentifierIsEmptyGuid()
    {
        // Arrange
        var principal = CreateAuthenticatedPrincipal(
            Guid.Empty.ToString());

        var currentUser = CreateCurrentUser(principal);

        // Act
        var act = () => currentUser.UserId;

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "The authenticated user does not have a valid user identifier.");
    }

    private static HttpCurrentUser CreateCurrentUser(
        ClaimsPrincipal principal)
    {
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        return new HttpCurrentUser(httpContextAccessor);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(
        string userId)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId)
            ],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}