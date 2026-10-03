using System.Net;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Infrastructure.Persistence;
using AiEngineeringManagerCopilot.IntegrationTests.Helpers;
using AiEngineeringManagerCopilot.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AiEngineeringManagerCopilot.IntegrationTests.Teams;

public sealed class TeamPaginationEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task GetPage_ShouldFilterCountAndPageOnlyCurrentUsersTeams()
    {
        var prefix = $"Paging-{Guid.NewGuid():N}";
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var other = new User
        {
            Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.com",
            Name = "Other owner", CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(other);
        db.Teams.AddRange(
            NewTeam($"{prefix}-C", OwnerId),
            NewTeam($"{prefix}-A", OwnerId),
            NewTeam($"{prefix}-B", OwnerId),
            NewTeam($"{prefix}-Foreign", other.Id));
        await db.SaveChangesAsync();

        var first = await ReadPageAsync($"pageNumber=1&pageSize=2&search={prefix.ToUpperInvariant()}");
        var second = await ReadPageAsync($"pageNumber=2&pageSize=2&search={prefix}");

        first.TotalCount.Should().Be(3);
        first.PageNumber.Should().Be(1);
        first.PageSize.Should().Be(2);
        first.Items.Select(team => team.Name).Should().Equal($"{prefix}-A", $"{prefix}-B");
        second.TotalCount.Should().Be(3);
        second.Items.Should().ContainSingle().Which.Name.Should().Be($"{prefix}-C");
        first.Items.Concat(second.Items).Should().OnlyContain(team => team.OwnerUserId == OwnerId);
    }

    [Fact]
    public async Task GetPage_ShouldOrderDuplicateNamesByIdWithoutRepeatingItems()
    {
        var name = $"Paging-{Guid.NewGuid():N}";
        var teams = new[] { NewTeam(name, OwnerId), NewTeam(name, OwnerId) };
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Teams.AddRange(teams);
        await db.SaveChangesAsync();

        var first = await ReadPageAsync($"pageNumber=1&pageSize=1&search={name}");
        var second = await ReadPageAsync($"pageNumber=2&pageSize=1&search={name}");
        first.Items.Single().Id.Should().NotBe(second.Items.Single().Id);
        new[] { first.Items.Single().Id, second.Items.Single().Id }
            .Should().Equal(teams.OrderBy(team => team.Id).Select(team => team.Id));
    }

    [Fact]
    public async Task GetPage_ShouldTreatSearchWildcardsAsLiteralText()
    {
        var prefix = $"Paging-{Guid.NewGuid():N}";
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Teams.AddRange(NewTeam($"{prefix}%_Exact", OwnerId), NewTeam($"{prefix}-Different", OwnerId));
        await db.SaveChangesAsync();

        var page = await ReadPageAsync($"search={Uri.EscapeDataString(prefix + "%_")}");
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle().Which.Name.Should().Be($"{prefix}%_Exact");
    }

    [Theory]
    [InlineData("pageNumber=0")]
    [InlineData("pageNumber=-1")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageNumber=2147483647&pageSize=100")]
    [InlineData("pageNumber=not-a-number")]
    public async Task GetPage_ShouldRejectInvalidPagination(string query) =>
        (await _client.GetAsync($"/teams/paged?{query}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

    [Fact]
    public async Task GetPage_ShouldUseDefaultsAndReturnAnEmptyPageForNoMatches()
    {
        var page = await ReadPageAsync($"search=NoTeam-{Guid.NewGuid():N}");
        page.PageNumber.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
        var oldEndpoint = await _client.GetAsync("/teams");
        oldEndpoint.StatusCode.Should().Be(HttpStatusCode.OK);
        (await oldEndpoint.Content.ReadAsStringAsync()).TrimStart().Should().StartWith("[");
    }

    private async Task<PagedResult<TeamResponse>> ReadPageAsync(string query)
    {
        var response = await _client.GetAsync($"/teams/paged?{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadApiJsonAsync<PagedResult<TeamResponse>>())!;
    }

    private static Team NewTeam(string name, Guid ownerId) => new()
    {
        Id = Guid.NewGuid(), Name = name, OwnerUserId = ownerId, CreatedAt = DateTimeOffset.UtcNow
    };
}
