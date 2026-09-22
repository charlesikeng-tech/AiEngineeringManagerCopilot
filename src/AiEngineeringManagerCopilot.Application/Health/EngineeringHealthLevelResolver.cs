namespace AiEngineeringManagerCopilot.Application.Health;

public static class EngineeringHealthLevelResolver
{
    public static string Resolve(
        int score,
        decimal dataCoverage)
    {
        if (dataCoverage == 0)
            return "No Data";

        if (score >= 90) return "Excellent";
        if (score >= 75) return "Healthy";
        if (score >= 60) return "Needs Attention";
        if (score >= 40) return "At Risk";

        return "Critical";
    }
}