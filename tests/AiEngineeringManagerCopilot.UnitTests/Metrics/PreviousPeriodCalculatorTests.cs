using AiEngineeringManagerCopilot.Application.Metrics;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Metrics;

public sealed class PreviousPeriodCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnPreviousPeriodWithSameDuration()
    {
        var calculator = new PreviousPeriodCalculator();

        var result = calculator.Calculate(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        result.Start.Should().Be(
            new DateOnly(2026, 8, 2));

        result.End.Should().Be(
            new DateOnly(2026, 8, 31));
    }
    
    [Fact]
    public void Calculate_ShouldThrow_WhenPeriodStartIsAfterPeriodEnd()
    {
        var calculator = new PreviousPeriodCalculator();

        var periodStart = new DateOnly(2026, 9, 10);
        var periodEnd = new DateOnly(2026, 9, 1);

        var act = () => calculator.Calculate(
            periodStart,
            periodEnd);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Period start must be before or equal to period end.");
    }
    
    [Fact]
    public void Calculate_ShouldReturnPreviousDay_WhenPeriodIsOneDay()
    {
        var calculator = new PreviousPeriodCalculator();

        var result = calculator.Calculate(
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 10));

        result.Start.Should().Be(
            new DateOnly(2026, 9, 9));

        result.End.Should().Be(
            new DateOnly(2026, 9, 9));
    }
    
    [Fact]
    public void Calculate_ShouldReturnPreviousDay_ForOneDayPeriod()
    {
        var calculator = new PreviousPeriodCalculator();

        var result = calculator.Calculate(
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 10));

        result.Start.Should().Be(
            new DateOnly(2026, 9, 9));

        result.End.Should().Be(
            new DateOnly(2026, 9, 9));
    }
}