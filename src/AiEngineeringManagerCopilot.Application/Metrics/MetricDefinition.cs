using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Metrics;


public static class MetricDefinitions
{
    private static readonly IReadOnlyDictionary<MetricType, MetricDefinition>
        Definitions =
            new Dictionary<MetricType, MetricDefinition>
            {
                [MetricType.LeadTime] = new(
                    MetricType.LeadTime,
                    "hours",
                    MetricTemporalSemantics.ReportingPeriod,
                    "Average time from Jira issue creation to resolution " +
                    "for issues completed during the reporting period."),

                [MetricType.BlockedItems] = new(
                    MetricType.BlockedItems,
                    "items",
                    MetricTemporalSemantics.CurrentSnapshot,
                    "Number of Jira work items whose current status is Blocked.")
            };

    public static MetricDefinition? Find(MetricType metricType)
    {
        return Definitions.GetValueOrDefault(metricType);
    }
}

public enum MetricTemporalSemantics
{
    ReportingPeriod,
    CurrentSnapshot
}

public sealed record MetricDefinition(
    MetricType MetricType,
    string Unit,
    MetricTemporalSemantics TemporalSemantics,
    string Description);