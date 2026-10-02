using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Metrics;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class EngineeringMetricsServiceTests
{
    private static readonly DateOnly Start = new(2026, 9, 1);
    private static readonly DateOnly End = new(2026, 9, 30);
    private static readonly MetricType[] MetricOrder =
    [
        MetricType.CycleTime, MetricType.PRReviewTime,
        MetricType.DeploymentFrequency, MetricType.ChangeFailureRate,
        MetricType.LeadTime, MetricType.OpenPRs,
        MetricType.MergedPRs, MetricType.BlockedItems
    ];

    private static readonly string[] AllLoads =
    [
        "Team", "GitHub", "Jira", "MergedPRs", "PeriodPRs",
        "Reviews", "Deployments", "CompletedItems", "OpenPRs", "WorkItems"
    ];

    public static IEnumerable<object?[]> Calculations()
    {
        yield return [null];
        foreach (var type in MetricOrder)
            yield return [type];
    }

    public static IEnumerable<object[]> SingleCalculations()
    {
        yield return [MetricType.CycleTime, new[] { "Team", "GitHub", "MergedPRs" }];
        yield return [MetricType.PRReviewTime, new[] { "Team", "GitHub", "PeriodPRs", "Reviews" }];
        yield return [MetricType.DeploymentFrequency, new[] { "Team", "GitHub", "Deployments" }];
        yield return [MetricType.ChangeFailureRate, new[] { "Team", "GitHub", "Deployments" }];
        yield return [MetricType.LeadTime, new[] { "Team", "Jira", "CompletedItems" }];
        yield return [MetricType.OpenPRs, new[] { "Team", "GitHub", "OpenPRs" }];
        yield return [MetricType.MergedPRs, new[] { "Team", "GitHub", "PeriodPRs" }];
        yield return [MetricType.BlockedItems, new[] { "Team", "Jira", "WorkItems" }];
    }

    [Fact]
    public async Task CalculateAllAsync_LoadsEachSourceOnce_AndPersistsEightMetricsInOriginalOrder()
    {
        var fixture = new Fixture();
        fixture.AddRepresentativeData();
        using var cancellation = new CancellationTokenSource();

        var results = await fixture.Service.CalculateAllAsync(
            fixture.TeamId, Start, End, cancellation.Token);

        results.Should().NotBeNull();
        fixture.Repositories.Calls.Where(x => x.Name != "Save")
            .Select(x => x.Name).Should().BeEquivalentTo(AllLoads);
        fixture.Repositories.Saved.Select(x => x.MetricType).Should().Equal(MetricOrder);
        results!.Select(x => x.MetricType).Should().Equal(MetricOrder);
        results.Select(x => x.Value).Should().Equal(
            new decimal?[] { 36m, 9m, 2m, 33.33m, 60m, 2m, 1m, 2m });
        results.Should().OnlyContain(x => x.DataStatus == MetricDataStatus.Available);
        fixture.Repositories.Calls.Should().OnlyContain(x => x.Token == cancellation.Token);
        AssertQueryArguments(fixture, fixture.TeamId, Start, End);
        AssertPersistedResults(fixture, results);
    }

    [Theory]
    [MemberData(nameof(SingleCalculations))]
    public async Task SingleCalculation_LoadsOnlyItsRequiredSourceAndData(
        MetricType metricType, string[] expectedLoads)
    {
        var fixture = new Fixture();
        fixture.AddRepresentativeData();

        var result = await fixture.CalculateAsync(metricType);

        result.Should().NotBeNull();
        fixture.Repositories.Calls.Where(x => x.Name != "Save")
            .Select(x => x.Name).Should().BeEquivalentTo(expectedLoads);
        fixture.Repositories.Saved.Should().ContainSingle()
            .Which.MetricType.Should().Be(metricType);
        AssertQueryArguments(fixture, fixture.TeamId, Start, End);
        AssertPersistedResults(fixture, result!);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CalculateAllAsync_MatchesEightSingles_WithRepresentativeData(
        bool gitHubConfigured, bool jiraConfigured)
    {
        var allFixture = new Fixture();
        allFixture.AddRepresentativeData();
        allFixture.Repositories.GitHubConfigured = gitHubConfigured;
        allFixture.Repositories.JiraConfigured = jiraConfigured;

        var all = (await allFixture.CalculateAsync(null))!;
        var singles = new List<EngineeringMetricResponse>();
        foreach (var metricType in MetricOrder)
        {
            var singleFixture = new Fixture();
            singleFixture.AddRepresentativeData();
            singleFixture.Repositories.GitHubConfigured = gitHubConfigured;
            singleFixture.Repositories.JiraConfigured = jiraConfigured;
            singles.Add((await singleFixture.CalculateAsync(metricType))!.Single());
        }

        all.Select(x => (x.MetricType, x.Value, x.DataStatus))
            .Should().Equal(singles.Select(x => (x.MetricType, x.Value, x.DataStatus)));
        all.Where(x => IsGitHubMetric(x.MetricType) ? !gitHubConfigured : !jiraConfigured)
            .Should().OnlyContain(x =>
                x.Value == null && x.DataStatus == MetricDataStatus.SourceNotConfigured);
        all.Where(x => IsGitHubMetric(x.MetricType) ? gitHubConfigured : jiraConfigured)
            .Should().OnlyContain(x => x.DataStatus == MetricDataStatus.Available);
        var loadNames = allFixture.Repositories.Calls.Select(x => x.Name).ToArray();
        if (!gitHubConfigured)
            loadNames.Should().NotContain(new[] { "MergedPRs", "PeriodPRs", "Reviews", "Deployments", "OpenPRs" });
        if (!jiraConfigured)
            loadNames.Should().NotContain(new[] { "CompletedItems", "WorkItems" });
    }

    [Theory]
    [MemberData(nameof(Calculations))]
    public async Task Calculation_WithEmptyConfiguredSources_PreservesNoDataAndAvailableZero(
        MetricType? metricType)
    {
        var fixture = new Fixture();

        var results = await fixture.CalculateAsync(metricType);

        results.Should().NotBeNull();
        foreach (var result in results!)
        {
            var noData = result.MetricType is MetricType.CycleTime or MetricType.PRReviewTime
                or MetricType.ChangeFailureRate or MetricType.LeadTime;
            result.DataStatus.Should().Be(noData ? MetricDataStatus.NoData : MetricDataStatus.Available);
            result.Value.Should().Be(noData ? null : 0m);
        }
        AssertPersistedResults(fixture, results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PRReviewTime_WithPullRequestsButNoValidReview_PersistsNoData(bool calculateAll)
    {
        var fixture = new Fixture();
        fixture.AddRepresentativeData();
        fixture.Repositories.Reviews.Clear();
        fixture.Repositories.Reviews.Add(new PullRequestReview
        {
            PullRequestId = fixture.Repositories.PeriodPullRequests[0].Id,
            SubmittedAt = fixture.Repositories.PeriodPullRequests[0].CreatedAt.AddHours(-1)
        });

        var results = (await fixture.CalculateAsync(calculateAll ? null : MetricType.PRReviewTime))!;

        var result = results.Single(x => x.MetricType == MetricType.PRReviewTime);
        result.Value.Should().BeNull();
        result.DataStatus.Should().Be(MetricDataStatus.NoData);
        fixture.Repositories.Calls.Count(x => x.Name == "PeriodPRs").Should().Be(1);
        fixture.Repositories.Calls.Count(x => x.Name == "Reviews").Should().Be(1);
        AssertPersistedResults(fixture, results);
    }

    [Theory]
    [MemberData(nameof(Calculations))]
    public async Task Calculation_WithoutConnections_DoesNotLoadData_AndPersistsSourceNotConfigured(
        MetricType? metricType)
    {
        var fixture = new Fixture();
        fixture.Repositories.GitHubConfigured = false;
        fixture.Repositories.JiraConfigured = false;

        var results = await fixture.CalculateAsync(metricType);

        results.Should().NotBeNull();
        results!.Should().HaveCount(metricType.HasValue ? 1 : 8);
        results.Should().OnlyContain(x => x.Value == null &&
            x.DataStatus == MetricDataStatus.SourceNotConfigured);
        var expected = metricType.HasValue
            ? new[] { "Team", IsGitHubMetric(metricType.Value) ? "GitHub" : "Jira" }
            : new[] { "Team", "GitHub", "Jira" };
        fixture.Repositories.Calls.Where(x => x.Name != "Save")
            .Select(x => x.Name).Should().BeEquivalentTo(expected);
        AssertPersistedResults(fixture, results);
    }

    [Theory]
    [MemberData(nameof(Calculations))]
    public async Task Calculation_WhenTeamMissingOrOwnedByAnotherUser_ReturnsNullWithoutOtherAccess(
        MetricType? metricType)
    {
        foreach (var missing in new[] { true, false })
        {
            var fixture = new Fixture();
            if (missing)
                fixture.Repositories.Teams.Clear();
            else
                fixture.Repositories.Teams[fixture.TeamId].OwnerUserId = Guid.NewGuid();

            var result = await fixture.CalculateAsync(metricType);

            result.Should().BeNull();
            var call = fixture.Repositories.Calls.Should().ContainSingle().Which;
            call.Name.Should().Be("Team");
            call.TeamId.Should().Be(fixture.TeamId);
            call.OwnerUserId.Should().Be(fixture.UserId);
            fixture.Repositories.Saved.Should().BeEmpty();
        }
    }

    [Theory]
    [MemberData(nameof(Calculations))]
    public async Task Calculation_WithInvalidPeriod_ThrowsBeforeAnyRepositoryAccess(
        MetricType? metricType)
    {
        var fixture = new Fixture();

        var act = () => fixture.CalculateAsync(metricType, start: End, end: Start);

        await act.Should().ThrowAsync<ArgumentException>();
        fixture.Repositories.Calls.Should().BeEmpty();
        fixture.Repositories.Saved.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CalculateAllAsync_UsesFreshDataForRepeatedCallsTeamsAndPeriods(
        bool differentTeam, bool differentPeriod)
    {
        var fixture = new Fixture();
        fixture.AddRepresentativeData();
        var first = await fixture.CalculateAsync(null);
        first!.Single(x => x.MetricType == MetricType.DeploymentFrequency).Value.Should().Be(2m);
        fixture.Repositories.Calls.Clear();
        fixture.Repositories.Saved.Clear();
        fixture.Repositories.Deployments.Clear();
        fixture.Repositories.JiraConfigured = false;
        var teamId = differentTeam ? Guid.NewGuid() : fixture.TeamId;
        fixture.Repositories.Teams[teamId] = new Team { Id = teamId, OwnerUserId = fixture.UserId };
        var start = differentPeriod ? Start.AddMonths(1) : Start;
        var end = differentPeriod ? End.AddMonths(1) : End;

        var second = (await fixture.CalculateAsync(null, teamId, start, end))!;

        second.Single(x => x.MetricType == MetricType.DeploymentFrequency).Value.Should().Be(0m);
        second.Single(x => x.MetricType == MetricType.ChangeFailureRate).DataStatus.Should().Be(MetricDataStatus.NoData);
        second.Where(x => !IsGitHubMetric(x.MetricType)).Should().OnlyContain(x =>
            x.Value == null && x.DataStatus == MetricDataStatus.SourceNotConfigured);
        fixture.Repositories.Calls.Where(x => x.Name != "Save").Select(x => x.Name)
            .Should().BeEquivalentTo(AllLoads.Except(new[] { "CompletedItems", "WorkItems" }));
        AssertQueryArguments(fixture, teamId, start, end);
        AssertPersistedResults(fixture, second);
    }

    [Theory]
    [MemberData(nameof(Calculations))]
    public async Task Calculation_PropagatesCancellationToEveryRepository(
        MetricType? metricType)
    {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();

        await fixture.CalculateAsync(metricType, token: cancellation.Token);

        fixture.Repositories.Calls.Should().OnlyContain(x => x.Token == cancellation.Token);
        fixture.Repositories.Calls.Clear();
        fixture.Repositories.Saved.Clear();
        cancellation.Cancel();
        var act = () => fixture.CalculateAsync(metricType, token: cancellation.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        fixture.Repositories.Calls.Should().ContainSingle().Which.Token.Should().Be(cancellation.Token);
        fixture.Repositories.Saved.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateAllAsync_WhenDataLoadIsCancelled_DoesNotPersistPartialMetrics()
    {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        fixture.Repositories.BeforeCall = name =>
        {
            if (name == "Deployments")
                cancellation.Cancel();
        };

        var act = () => fixture.CalculateAsync(null, token: cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        fixture.Repositories.Calls.Last().Name.Should().Be("Deployments");
        fixture.Repositories.Calls.Should().OnlyContain(x => x.Token == cancellation.Token);
        fixture.Repositories.Saved.Should().BeEmpty();
    }

    private static bool IsGitHubMetric(MetricType type) =>
        type is not (MetricType.LeadTime or MetricType.BlockedItems);

    private static void AssertQueryArguments(Fixture fixture, Guid teamId, DateOnly start, DateOnly end)
    {
        var calls = fixture.Repositories.Calls;
        calls.Where(x => x.Name != "Reviews").Should().OnlyContain(x => x.TeamId == teamId);
        calls.Single(x => x.Name == "Team").OwnerUserId.Should().Be(fixture.UserId);
        calls.Where(x => x.Name is "MergedPRs" or "PeriodPRs" or "Deployments")
            .Should().OnlyContain(x => x.Start == start && x.End == end);
        foreach (var call in calls.Where(x => x.Name == "OpenPRs"))
            call.End.Should().Be(end);
        foreach (var call in calls.Where(x => x.Name == "CompletedItems"))
        {
            call.From.Should().Be(new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
            call.To.Should().Be(new DateTimeOffset(end.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero));
            call.From!.Value.Offset.Should().Be(TimeSpan.Zero);
            call.To!.Value.Offset.Should().Be(TimeSpan.Zero);
        }
        foreach (var call in calls.Where(x => x.Name == "Reviews"))
            call.PullRequestIds.Should().Equal(fixture.Repositories.PeriodPullRequests.Select(x => x.Id));
        fixture.Repositories.Saved.Should().OnlyContain(x =>
            x.TeamId == teamId && x.PeriodStart == start && x.PeriodEnd == end);
    }

    private static void AssertPersistedResults(
        Fixture fixture, IReadOnlyList<EngineeringMetricResponse> results)
    {
        fixture.Repositories.Saved.Should().HaveCount(results.Count);
        fixture.Repositories.Calls.Count(x => x.Name == "Save").Should().Be(results.Count);
        for (var i = 0; i < results.Count; i++)
        {
            var saved = fixture.Repositories.Saved[i];
            var result = results[i];
            result.Id.Should().Be(saved.Id).And.NotBeEmpty();
            result.TeamId.Should().Be(saved.TeamId);
            result.MetricType.Should().Be(saved.MetricType);
            result.Value.Should().Be(saved.Value);
            result.DataStatus.Should().Be(saved.DataStatus);
            result.PeriodStart.Should().Be(saved.PeriodStart);
            result.PeriodEnd.Should().Be(saved.PeriodEnd);
            result.CreatedAt.Should().Be(saved.CreatedAt);
            saved.CreatedAt.Should().NotBe(default);
            saved.UpdatedAt.Should().Be(saved.CreatedAt);
        }
    }

    private sealed class Fixture
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid TeamId { get; } = Guid.NewGuid();
        public FakeRepositories Repositories { get; } = new();
        public EngineeringMetricsService Service { get; }

        public Fixture()
        {
            Repositories.Teams[TeamId] = new Team { Id = TeamId, OwnerUserId = UserId };
            Service = new EngineeringMetricsService(
                new FakeCurrentUser(UserId), Repositories, Repositories, Repositories,
                Repositories, Repositories, new CycleTimeCalculator(), new PRReviewTimeCalculator(),
                Repositories, new DeploymentFrequencyCalculator(), new ChangeFailureRateCalculator(),
                new LeadTimeCalculator(), new OpenPullRequestsCalculator(),
                new MergedPullRequestsCalculator(), new BlockedItemsCalculator(), Repositories, Repositories);
        }

        public async Task<IReadOnlyList<EngineeringMetricResponse>?> CalculateAsync(
            MetricType? type, Guid? teamId = null, DateOnly? start = null, DateOnly? end = null,
            CancellationToken token = default)
        {
            var id = teamId ?? TeamId;
            var from = start ?? Start;
            var to = end ?? End;
            if (type is null)
                return await Service.CalculateAllAsync(id, from, to, token);
            var result = type switch
            {
                MetricType.CycleTime => await Service.CalculateCycleTimeAsync(id, from, to, token),
                MetricType.PRReviewTime => await Service.CalculatePRReviewTimeAsync(id, from, to, token),
                MetricType.DeploymentFrequency => await Service.CalculateDeploymentFrequencyAsync(id, from, to, token),
                MetricType.ChangeFailureRate => await Service.CalculateChangeFailureRateAsync(id, from, to, token),
                MetricType.LeadTime => await Service.CalculateLeadTimeAsync(id, from, to, token),
                MetricType.OpenPRs => await Service.CalculateOpenPullRequestsAsync(id, from, to, token),
                MetricType.MergedPRs => await Service.CalculateMergedPullRequestsAsync(id, from, to, token),
                MetricType.BlockedItems => await Service.CalculateBlockedItemsAsync(id, from, to, token),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
            return result is null ? null : [result];
        }

        public void AddRepresentativeData()
        {
            var first = new PullRequest
            {
                Id = Guid.NewGuid(), State = PullRequestState.Merged,
                CreatedAt = Utc(9, 2), MergedAt = Utc(9, 3), ClosedAt = Utc(9, 3)
            };
            var carried = new PullRequest
            {
                Id = Guid.NewGuid(), State = PullRequestState.Merged,
                CreatedAt = Utc(8, 31), MergedAt = Utc(9, 2), ClosedAt = Utc(9, 2)
            };
            var open = new PullRequest { Id = Guid.NewGuid(), CreatedAt = Utc(9, 25), State = PullRequestState.Open };
            Repositories.MergedPullRequests.AddRange([first, carried]);
            Repositories.PeriodPullRequests.AddRange([first, carried, open]);
            Repositories.Reviews.AddRange(
            [
                new PullRequestReview { PullRequestId = first.Id, SubmittedAt = first.CreatedAt.AddHours(-1) },
                new PullRequestReview { PullRequestId = first.Id, SubmittedAt = first.CreatedAt.AddHours(6) },
                new PullRequestReview { PullRequestId = first.Id, SubmittedAt = first.CreatedAt.AddHours(18) },
                new PullRequestReview { PullRequestId = carried.Id, SubmittedAt = carried.CreatedAt.AddHours(12) }
            ]);
            Repositories.Deployments.AddRange(
            [
                new Deployment { DeployedAt = Utc(9, 1), Status = "success" },
                new Deployment { DeployedAt = Utc(9, 30).AddHours(23), Status = "SUCCESS" },
                new Deployment { DeployedAt = Utc(9, 15), Status = "FAILURE" }
            ]);
            Repositories.OpenPullRequests.AddRange(
            [
                open,
                new PullRequest { Id = Guid.NewGuid(), CreatedAt = Utc(8, 15), ClosedAt = Utc(10, 1) }
            ]);
            Repositories.CompletedItems.AddRange(
            [
                new JiraWorkItem { TeamId = TeamId, CreatedAt = Utc(8, 31), DoneAt = Utc(9, 3) },
                new JiraWorkItem { TeamId = TeamId, CreatedAt = Utc(9, 10), DoneAt = Utc(9, 12) }
            ]);
            Repositories.WorkItems.AddRange(
            [
                new JiraWorkItem { TeamId = TeamId, CreatedAt = Utc(8, 1), IsBlocked = true },
                new JiraWorkItem { TeamId = TeamId, CreatedAt = Utc(9, 20), IsBlocked = true },
                new JiraWorkItem { TeamId = TeamId, CreatedAt = Utc(9, 20), IsBlocked = false }
            ]);
        }

        private static DateTimeOffset Utc(int month, int day) => new(2026, month, day, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed record RepositoryCall(
        string Name, CancellationToken Token, Guid? TeamId = null,
        Guid? OwnerUserId = null, DateOnly? Start = null, DateOnly? End = null,
        DateTimeOffset? From = null, DateTimeOffset? To = null,
        IReadOnlyCollection<Guid>? PullRequestIds = null);

    private sealed class FakeRepositories :
        ITeamRepository, IGitHubConnectionRepository, IJiraConnectionRepository,
        IPullRequestRepository, IPullRequestReviewRepository, IDeploymentRepository,
        IJiraWorkItemRepository, IEngineeringMetricRepository
    {
        public Dictionary<Guid, Team> Teams { get; } = [];
        public bool GitHubConfigured { get; set; } = true;
        public bool JiraConfigured { get; set; } = true;
        public List<PullRequest> MergedPullRequests { get; } = [];
        public List<PullRequest> PeriodPullRequests { get; } = [];
        public List<PullRequest> OpenPullRequests { get; } = [];
        public List<PullRequestReview> Reviews { get; } = [];
        public List<Deployment> Deployments { get; } = [];
        public List<JiraWorkItem> CompletedItems { get; } = [];
        public List<JiraWorkItem> WorkItems { get; } = [];
        public List<EngineeringMetric> Saved { get; } = [];
        public List<RepositoryCall> Calls { get; } = [];
        public Action<string>? BeforeCall { get; set; }

        private void Record(RepositoryCall call)
        {
            Calls.Add(call);
            BeforeCall?.Invoke(call.Name);
            call.Token.ThrowIfCancellationRequested();
        }

        public Task<Team?> GetByIdAsync(Guid teamId, Guid ownerUserId, CancellationToken cancellationToken)
        {
            Record(new("Team", cancellationToken, teamId, ownerUserId));
            return Task.FromResult(Teams.TryGetValue(teamId, out var team) && team.OwnerUserId == ownerUserId
                ? team : null);
        }

        Task<GitHubConnection?> IGitHubConnectionRepository.GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken)
        {
            Record(new("GitHub", cancellationToken, teamId));
            return Task.FromResult(GitHubConfigured ? new GitHubConnection { TeamId = teamId } : null);
        }

        Task<JiraConnection?> IJiraConnectionRepository.GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken)
        {
            Record(new("Jira", cancellationToken, teamId));
            return Task.FromResult(JiraConfigured ? new JiraConnection { TeamId = teamId } : null);
        }

        public Task<IReadOnlyList<PullRequest>> GetMergedByTeamAndPeriodAsync(
            Guid teamId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken)
        {
            Record(new("MergedPRs", cancellationToken, teamId, Start: periodStart, End: periodEnd));
            return Task.FromResult<IReadOnlyList<PullRequest>>(MergedPullRequests.ToArray());
        }

        Task<IReadOnlyList<PullRequest>> IPullRequestRepository.GetByTeamAndPeriodAsync(
            Guid teamId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken)
        {
            Record(new("PeriodPRs", cancellationToken, teamId, Start: periodStart, End: periodEnd));
            return Task.FromResult<IReadOnlyList<PullRequest>>(PeriodPullRequests.ToArray());
        }

        public Task<IReadOnlyList<PullRequest>> GetOpenByTeamAtDateAsync(
            Guid teamId, DateOnly date, CancellationToken cancellationToken)
        {
            Record(new("OpenPRs", cancellationToken, teamId, End: date));
            return Task.FromResult<IReadOnlyList<PullRequest>>(OpenPullRequests.ToArray());
        }

        public Task<IReadOnlyList<PullRequestReview>> GetByPullRequestIdsAsync(
            IReadOnlyCollection<Guid> pullRequestIds, CancellationToken cancellationToken)
        {
            Record(new("Reviews", cancellationToken, PullRequestIds: pullRequestIds.ToArray()));
            return Task.FromResult<IReadOnlyList<PullRequestReview>>(Reviews.ToArray());
        }

        Task<IReadOnlyList<Deployment>> IDeploymentRepository.GetByTeamAndPeriodAsync(
            Guid teamId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken)
        {
            Record(new("Deployments", cancellationToken, teamId, Start: periodStart, End: periodEnd));
            return Task.FromResult<IReadOnlyList<Deployment>>(Deployments.ToArray());
        }

        public Task<IReadOnlyList<JiraWorkItem>> GetCompletedByTeamAndPeriodAsync(
            Guid teamId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        {
            Record(new("CompletedItems", cancellationToken, teamId, From: from, To: to));
            return Task.FromResult<IReadOnlyList<JiraWorkItem>>(CompletedItems.ToArray());
        }

        public Task<IReadOnlyList<JiraWorkItem>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken)
        {
            Record(new("WorkItems", cancellationToken, teamId));
            return Task.FromResult<IReadOnlyList<JiraWorkItem>>(WorkItems.ToArray());
        }

        public Task<EngineeringMetric> UpsertAsync(EngineeringMetric metric, CancellationToken cancellationToken)
        {
            Record(new("Save", cancellationToken, metric.TeamId));
            Saved.Add(metric);
            return Task.FromResult(metric);
        }

        public Task<IReadOnlyList<Team>> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(Team team, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Team team, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(GitHubConnection connection, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(GitHubConnection connection, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<IReadOnlyList<GitHubConnection>> IGitHubConnectionRepository.GetAllAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(JiraConnection connection, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(JiraConnection connection, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<IReadOnlyList<JiraConnection>> IJiraConnectionRepository.GetAllAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<PullRequest?> IPullRequestRepository.GetByExternalIdAsync(Guid repositoryId, long externalId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PullRequest> UpsertAsync(PullRequest pullRequest, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(PullRequest pullRequest, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<PullRequestReview?> IPullRequestReviewRepository.GetByExternalIdAsync(Guid pullRequestId, long externalId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PullRequestReview> UpsertAsync(PullRequestReview review, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(PullRequestReview review, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<Deployment?> IDeploymentRepository.GetByExternalIdAsync(Guid repositoryId, long externalId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Deployment> UpsertAsync(Deployment deployment, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(Deployment deployment, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<JiraWorkItem?> GetByExternalIdAsync(Guid teamId, string externalId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<JiraWorkItem> UpsertAsync(JiraWorkItem workItem, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(JiraWorkItem workItem, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<JiraWorkItem>> GetByTeamAndPeriodAsync(Guid teamId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(EngineeringMetric metric, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringMetric?> GetByTeamAndPeriodAsync(Guid teamId, MetricType metricType, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<EngineeringMetric>> GetLatestByTeamAsync(Guid teamId, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<IReadOnlyList<EngineeringMetric>> IEngineeringMetricRepository.GetByTeamAndPeriodAsync(Guid teamId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
