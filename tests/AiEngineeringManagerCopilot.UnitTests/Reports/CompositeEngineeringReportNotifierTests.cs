using AiEngineeringManagerCopilot.Application.Reports;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiEngineeringManagerCopilot.UnitTests.Reports;

public sealed class CompositeEngineeringReportNotifierTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NotifyCreatedAsync_ShouldContinueAfterDeliveryFailure(bool timeout)
    {
        var first = new RecordingNotifier(timeout
            ? new TaskCanceledException("Timeout")
            : new HttpRequestException("Unavailable"));
        var second = new RecordingNotifier();
        var notifier = new CompositeEngineeringReportNotifier(
            [first, second], NullLogger<CompositeEngineeringReportNotifier>.Instance);
        var report = CreateReport();

        await notifier.NotifyCreatedAsync(report.TeamId, "Team", report, CancellationToken.None);

        first.Calls.Should().Be(1);
        second.Calls.Should().Be(1);
        second.Report.Should().BeSameAs(report);
    }

    [Fact]
    public async Task NotifyCreatedAsync_ShouldPropagateCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var first = new RecordingNotifier(new TaskCanceledException());
        var second = new RecordingNotifier();
        var notifier = new CompositeEngineeringReportNotifier(
            [first, second], NullLogger<CompositeEngineeringReportNotifier>.Instance);
        var report = CreateReport();

        var send = () => notifier.NotifyCreatedAsync(report.TeamId, "Team", report, cancellation.Token);
        await send.Should().ThrowAsync<TaskCanceledException>();
        second.Calls.Should().Be(0);
    }

    private static EngineeringReportResponse CreateReport() =>
        new(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            "Summary", 80, "Healthy", 100, DateTimeOffset.UtcNow, [], [], [], [], []);

    private sealed class RecordingNotifier(Exception? failure = null) : IEngineeringReportNotifier
    {
        public int Calls { get; private set; }
        public EngineeringReportResponse? Report { get; private set; }

        public Task NotifyCreatedAsync(
            Guid teamId, string teamName, EngineeringReportResponse report, CancellationToken cancellationToken)
        {
            Calls++;
            Report = report;
            if (failure is not null)
            {
                throw failure;
            }
            return Task.CompletedTask;
        }
    }
}
