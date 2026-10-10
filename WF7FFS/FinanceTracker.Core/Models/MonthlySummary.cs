namespace FinanceTracker.Core.Models;

// Immutable - Egy hónap bevételei, kiadásai és egyenlege.
public sealed record MonthlySummary(int Year, int Month, decimal TotalIncome, decimal TotalExpense)
{
    public decimal Balance => TotalIncome - TotalExpense;
}
