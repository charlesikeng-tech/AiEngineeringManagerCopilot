using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class EngineeringMetric
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public MetricType MetricType { get; set; }

    public decimal Value { get; set; }

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}