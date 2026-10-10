using FinanceTracker.Core.Exceptions;
using FinanceTracker.Core.Models;
using FinanceTracker.Core.Persistence;
using FinanceTracker.Core.Queries;
using FinanceTracker.Core.Services;

// Dummy
Console.OutputEncoding = System.Text.Encoding.UTF8;

string filePath = Path.Combine(Path.GetTempPath(), "FinanceTracker-demo", "step3.json");
File.Delete(filePath);

try
{
    TransactionService service = new(new JsonTransactionRepository(filePath));
    await service.LoadAsync();

    service.Add(Transaction.Create(new DateOnly(2026, 9, 1), 450000m, TransactionType.Income, "Fizetés", "Szeptemberi bér"));
    service.Add(Transaction.Create(new DateOnly(2026, 9, 12), 18500m, TransactionType.Expense, "Étkezés", "Heti bevásárlás"));
    service.Add(Transaction.Create(new DateOnly(2026, 9, 20), 7200m, TransactionType.Expense, "étkezés", "Pékség"));
    service.Add(Transaction.Create(new DateOnly(2026, 10, 3), 120000m, TransactionType.Expense, "Rezsi", "Albérlet"));
    service.Add(Transaction.Create(new DateOnly(2026, 10, 9), 2490m, TransactionType.Expense, "Szórakozás", "Mozi"));

    Console.WriteLine("Kiadások kategóriánként:");
    foreach (CategorySummary summary in service.Transactions.Expenses().SummarizeByCategory())
    {
        Console.WriteLine($"  {summary.Category,-12} {summary.Total,10:N0} Ft  ({summary.Count} db, átlag {summary.Average:N0} Ft)");
    }

    Console.WriteLine("Havi egyenleg:");
    foreach (MonthlySummary month in service.Transactions.MonthlySummaries())
    {
        Console.WriteLine($"  {month.Year}.{month.Month:00}  bevétel {month.TotalIncome:N0}  kiadás {month.TotalExpense:N0}  egyenleg {month.Balance:N0} Ft");
    }

    Console.WriteLine($"Legnagyobb kiadás: {service.Transactions.LargestExpense()?.Description}");

    service.Undo();
    Console.WriteLine($"Visszavonás után {service.Transactions.Count} tranzakció van, mentetlen változás: {service.HasUnsavedChanges}");

    await service.SaveAsync();
    Console.WriteLine($"Mentve, mentetlen változás: {service.HasUnsavedChanges}");

    service.Remove(Guid.NewGuid());
}
catch (FinanceTrackerException ex)
{
    Console.WriteLine($"Hiba: {ex.Message}");
}
