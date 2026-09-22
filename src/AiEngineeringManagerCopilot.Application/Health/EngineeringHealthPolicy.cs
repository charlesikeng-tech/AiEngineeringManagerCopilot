namespace AiEngineeringManagerCopilot.Application.Health;

public static class EngineeringHealthPolicy
{
    public static class CycleTime
    {
        public const decimal ExcellentMax = 8m;
        public const decimal HealthyMax = 24m;
        public const decimal NeedsAttentionMax = 48m;
        public const decimal AtRiskMax = 72m;
    }

    public static class PrReviewTime
    {
        public const decimal ExcellentMax = 4m;
        public const decimal HealthyMax = 12m;
        public const decimal NeedsAttentionMax = 24m;
        public const decimal AtRiskMax = 48m;
    }

    public static class DeploymentFrequency
    {
        public const int ExcellentMin = 20;
        public const int HealthyMin = 10;
        public const int NeedsAttentionMin = 5;
        public const int AtRiskMin = 1;
    }

    public static class ChangeFailureRate
    {
        public const decimal ExcellentMax = 5m;
        public const decimal HealthyMax = 10m;
        public const decimal NeedsAttentionMax = 20m;
        public const decimal AtRiskMax = 30m;
    }

    public static class LeadTime
    {
        public const decimal ExcellentMax = 24m;
        public const decimal HealthyMax = 48m;
        public const decimal NeedsAttentionMax = 72m;
        public const decimal AtRiskMax = 168m;
    }

    public static class OpenPullRequests
    {
        public const int ExcellentMax = 2;
        public const int HealthyMax = 5;
        public const int NeedsAttentionMax = 10;
        public const int AtRiskMax = 20;
    }

    public static class MergedPullRequests
    {
        public const int ExcellentMin = 20;
        public const int HealthyMin = 10;
        public const int NeedsAttentionMin = 5;
        public const int AtRiskMin = 1;
    }

    public static class BlockedItems
    {
        public const int ExcellentMax = 0;
        public const int HealthyMax = 2;
        public const int NeedsAttentionMax = 5;
        public const int AtRiskMax = 10;
    }
}