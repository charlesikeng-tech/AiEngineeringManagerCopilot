using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Application.Reports;

public static class EngineeringActionPolicy
{
    public static ActionPriority GetPriority(RiskCategory category)
    {
        return category switch
        {
            RiskCategory.Quality => ActionPriority.Critical,
            RiskCategory.Reliability => ActionPriority.Critical,

            RiskCategory.Delivery => ActionPriority.High,
            RiskCategory.Review => ActionPriority.High,

            RiskCategory.Process => ActionPriority.Medium,
            RiskCategory.Ownership => ActionPriority.Medium,
            RiskCategory.TechnicalDebt => ActionPriority.Medium,

            _ => ActionPriority.Low
        };
    }
}