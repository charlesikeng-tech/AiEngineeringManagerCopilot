using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.GitHub;

public class GitHubConnectionRepositoryTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GitHubConnectionRepositoryTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllGitHubConnections()
    {
        // Arrange
        using var scope =
            _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var firstTeamId = Guid.NewGuid();
        var secondTeamId = Guid.NewGuid();

        dbContext.GitHubConnections.AddRange(
            new GitHubConnection
            {
                Id = Guid.NewGuid(),
                TeamId = firstTeamId,
                Organization = "organization-one",
                AccessTokenEncrypted = "token-one",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new GitHubConnection
            {
                Id = Guid.NewGuid(),
                TeamId = secondTeamId,
                Organization = "organization-two",
                AccessTokenEncrypted = "token-two",
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var repository = scope.ServiceProvider
            .GetRequiredService<IGitHubConnectionRepository>();

        // Act
        var connections = await repository.GetAllAsync(
            CancellationToken.None);

        // Assert
        connections.Should().Contain(x =>
            x.TeamId == firstTeamId);

        connections.Should().Contain(x =>
            x.TeamId == secondTeamId);
    }
}