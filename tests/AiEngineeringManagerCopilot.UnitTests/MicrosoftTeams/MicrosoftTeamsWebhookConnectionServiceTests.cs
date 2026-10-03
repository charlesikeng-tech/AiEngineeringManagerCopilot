using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Application.MicrosoftTeams;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.IntegrationTests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiEngineeringManagerCopilot.UnitTests.MicrosoftTeams;

public sealed class MicrosoftTeamsWebhookConnectionServiceTests
{
    private const string Url =
        "https://test.logic.azure.com/workflows/id/triggers/manual/paths/invoke?sig=fake";

    [Fact]
    public async Task ConnectionLifecycle_ShouldEncryptTestAndDeleteWebhook()
    {
        var fixture = new Fixture();
        var result = await fixture.ConnectAsync();
        result!.TeamId.Should().Be(fixture.Team.Id);
        fixture.Connections.Connection!.WebhookUrlEncrypted.Should().Be("protected:" + Url);
        (await fixture.Service.GetAsync(fixture.Team.Id, CancellationToken.None))
            .Should().Be(result);

        var test = await fixture.Service.TestAsync(fixture.Team.Id, CancellationToken.None);
        test!.Success.Should().BeTrue();
        fixture.Client.LastWebhookUri!.AbsoluteUri.Should().Be(Url);
        fixture.Client.LastMessage.Should().Contain("test successful");

        (await fixture.Service.DeleteAsync(fixture.Team.Id, CancellationToken.None)).Should().BeTrue();
        (await fixture.Service.GetAsync(fixture.Team.Id, CancellationToken.None)).Should().BeNull();
        (await fixture.Service.TestAsync(fixture.Team.Id, CancellationToken.None)).Should().BeNull();
        fixture.Connections.Saves.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateConnection()
    {
        var fixture = new Fixture();
        await fixture.ConnectAsync();
        var create = () => fixture.ConnectAsync();
        await create.Should().ThrowAsync<ConflictException>();
        fixture.Connections.Saves.Should().Be(1);
    }

    [Fact]
    public async Task ConnectionOperations_ShouldRespectOwnership()
    {
        var fixture = new Fixture();
        await fixture.ConnectAsync();
        fixture.Team.OwnerUserId = Guid.NewGuid();

        (await fixture.ConnectAsync()).Should().BeNull();
        (await fixture.Service.GetAsync(fixture.Team.Id, CancellationToken.None)).Should().BeNull();
        (await fixture.Service.TestAsync(fixture.Team.Id, CancellationToken.None)).Should().BeNull();
        (await fixture.Service.DeleteAsync(fixture.Team.Id, CancellationToken.None)).Should().BeFalse();
        fixture.Client.LastWebhookUri.Should().BeNull();
        fixture.Connections.Saves.Should().Be(1);
    }

    [Theory]
    [InlineData("rejection")]
    [InlineData("network")]
    [InlineData("timeout")]
    public async Task TestAsync_ShouldReportDeliveryFailure(string failure)
    {
        var fixture = new Fixture();
        await fixture.ConnectAsync();
        fixture.Client.Result = false;
        fixture.Client.Failure = failure switch
        {
            "network" => new HttpRequestException("Unavailable"),
            "timeout" => new TaskCanceledException("Timeout"),
            _ => null
        };

        var result = await fixture.Service.TestAsync(fixture.Team.Id, CancellationToken.None);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task TestAsync_ShouldPropagateCancellation()
    {
        var fixture = new Fixture();
        await fixture.ConnectAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var send = () => fixture.Service.TestAsync(fixture.Team.Id, cancellation.Token);
        await send.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task NotifyCreatedAsync_ShouldSendReportAndTolerateNetworkFailure()
    {
        var fixture = new Fixture();
        await fixture.ConnectAsync();
        var report = new EngineeringReportResponse(
            Guid.NewGuid(), fixture.Team.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            "Summary", 80, "Healthy", 100, DateTimeOffset.UtcNow, [], [], [], [], []);
        await fixture.Service.NotifyCreatedAsync(
            fixture.Team.Id, fixture.Team.Name, report, CancellationToken.None);
        fixture.Client.LastMessage.Should().Contain("Platform").And.Contain("2026-09-01")
            .And.Contain("2026-09-30").And.Contain("80/100 (Healthy)");

        fixture.Client.Failure = new HttpRequestException("Unavailable");
        var notify = () => fixture.Service.NotifyCreatedAsync(
            fixture.Team.Id, fixture.Team.Name, report, CancellationToken.None);
        await notify.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TestAsync_ShouldRejectInvalidStoredUrlBeforeSending()
    {
        var fixture = new Fixture();
        await fixture.ConnectAsync();
        fixture.Connections.Connection!.WebhookUrlEncrypted = "protected:https://example.com";

        var send = () => fixture.Service.TestAsync(fixture.Team.Id, CancellationToken.None);
        await send.Should().ThrowAsync<InvalidOperationException>();
        fixture.Client.LastWebhookUri.Should().BeNull();
    }

    private sealed class Fixture
    {
        public Team Team { get; } = new()
        {
            Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Name = "Platform"
        };
        public ConnectionRepository Connections { get; } = new();
        public FakeMicrosoftTeamsWebhookClient Client { get; } = new();
        public MicrosoftTeamsWebhookConnectionService Service { get; }

        public Fixture()
        {
            Service = new MicrosoftTeamsWebhookConnectionService(
                new CurrentUser(Team.OwnerUserId), new TeamRepository(Team),
                Connections, new SecretProtector(), Client,
                NullLogger<MicrosoftTeamsWebhookConnectionService>.Instance);
        }

        public Task<MicrosoftTeamsWebhookConnectionResponse?> ConnectAsync() =>
            Service.CreateAsync(Team.Id, new CreateMicrosoftTeamsWebhookRequest(Url), CancellationToken.None);
    }

    private sealed class CurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
    }

    private sealed class SecretProtector : ISecretProtector
    {
        public string Protect(string value) => "protected:" + value;
        public string Unprotect(string value) => value["protected:".Length..];
    }

    private sealed class TeamRepository(Team team) : ITeamRepository
    {
        public Task<Team?> GetByIdAsync(Guid teamId, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult(team.Id == teamId && team.OwnerUserId == ownerUserId ? team : null);

        public Task<IReadOnlyList<Team>> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task AddAsync(Team value, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task DeleteAsync(Team value, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class ConnectionRepository : IMicrosoftTeamsWebhookConnectionRepository
    {
        public MicrosoftTeamsWebhookConnection? Connection { get; set; }
        public int Saves { get; private set; }

        public Task<MicrosoftTeamsWebhookConnection?> GetByTeamIdAsync(
            Guid teamId, CancellationToken cancellationToken) =>
            Task.FromResult(Connection?.TeamId == teamId ? Connection : null);

        public Task AddAsync(MicrosoftTeamsWebhookConnection connection, CancellationToken cancellationToken)
        {
            Connection = connection;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(MicrosoftTeamsWebhookConnection connection, CancellationToken cancellationToken)
        {
            Connection = null;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saves++;
            return Task.CompletedTask;
        }
    }
}
