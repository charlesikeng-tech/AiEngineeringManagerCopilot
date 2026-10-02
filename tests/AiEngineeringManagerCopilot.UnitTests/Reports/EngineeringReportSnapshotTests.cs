using AiEngineeringManagerCopilot.Application.AI;
using AiEngineeringManagerCopilot.Application.Reports;
using AiEngineeringManagerCopilot.Domain.Entities;
using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.UnitTests.Reports;

public sealed class EngineeringReportSnapshotTests
{
    [Fact]
    public void SnapshotPreservesMetricStatusAndScoreIndependentlyOfLiveData()
    {
        var live = new EngineeringMetric { MetricType = MetricType.CycleTime, Value = 12m,
            DataStatus = MetricDataStatus.Available };
        var report = new EngineeringReport { SnapshotJson = new EngineeringReportSnapshot(1,
            [new AIAnalysisMetricContext(live.MetricType, live.Value, live.DataStatus),
             new AIAnalysisMetricContext(MetricType.LeadTime, null, MetricDataStatus.NoData)],
            [new EngineeringReportMetricResponse(live.MetricType, 12m, 80)], []).Serialize() };
        live.Value = 500m;
        var snapshot = EngineeringReportSnapshot.Read(report)!;
        Assert.Equal(12m, snapshot.Metrics.Single().Value);
        Assert.Equal(80, snapshot.Metrics.Single().Score);
        Assert.Equal(MetricDataStatus.NoData, snapshot.MetricContext.Last().DataStatus);
    }

    [Fact]
    public void LegacyReportHasNoReconstructedSnapshot()
    {
        Assert.Null(EngineeringReportSnapshot.Read(new EngineeringReport()));
    }
}
