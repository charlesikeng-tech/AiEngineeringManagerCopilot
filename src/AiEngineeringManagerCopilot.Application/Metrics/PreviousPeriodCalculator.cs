namespace AiEngineeringManagerCopilot.Application.Metrics;

public sealed class PreviousPeriodCalculator
{
    public PreviousPeriod Calculate(
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        if (periodStart > periodEnd)
        {
            throw new ArgumentException(
                "Period start must be before or equal to period end.");
        }

        var durationInDays =
            periodEnd.DayNumber - periodStart.DayNumber + 1;

        var previousEnd =
            periodStart.AddDays(-1);

        var previousStart =
            previousEnd.AddDays(-(durationInDays - 1));

        return new PreviousPeriod(
            previousStart,
            previousEnd);
    }
}