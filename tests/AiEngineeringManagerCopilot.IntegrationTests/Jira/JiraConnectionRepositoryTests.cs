using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Jira;

public sealed class JiraConnectionRepositoryTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public JiraConnectionRepositoryTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllJiraConnections()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var firstTeamId = Guid.NewGuid();
        var secondTeamId = Guid.NewGuid();

        dbContext.JiraConnections.AddRange(
            new JiraConnection
            {
                Id = Guid.NewGuid(),
                TeamId = firstTeamId,
                BaseUrl = "https://jira-one.example.com",
                Email = "team-one@example.com",
                ApiTokenEncrypted = "token-one",
                ProjectKey = "ONE",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new JiraConnection
            {
                Id = Guid.NewGuid(),
                TeamId = secondTeamId,
                BaseUrl = "https://jira-two.example.com",
                Email = "team-two@example.com",
                ApiTokenEncrypted = "token-two",
                ProjectKey = "TWO",
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IJiraConnectionRepository>();

        // Act
        var connections =
            await repository.GetAllAsync(
                CancellationToken.None);

        // Assert
        connections.Should()
            .Contain(x => x.TeamId == firstTeamId);

        connections.Should()
            .Contain(x => x.TeamId == secondTeamId);
    }
}