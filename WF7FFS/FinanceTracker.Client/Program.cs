using FinanceTracker.Core.Exceptions;
using FinanceTracker.Core.Models;
using FinanceTracker.Core.Persistence;

// Dummy adat
Console.OutputEncoding = System.Text.Encoding.UTF8;

string filePath = Path.Combine(Path.GetTempPath(), "FinanceTracker-demo", "transactions.json");

try
{
    ITransactionRepository repository = new JsonTransactionRepository(filePath);

    List<Transaction> transactions = new()
    {
        Transaction.Create(new DateOnly(2026, 10, 1), 450000m, TransactionType.Income, "Fizetés", "Októberi bér"),
        Transaction.Create(new DateOnly(2026, 10, 9), 2490m, TransactionType.Expense, "Étkezés", "Ebéd az egyetemen")
    };

    await repository.SaveAsync(transactions);
    Console.WriteLine($"Mentve: {filePath}");

    IReadOnlyList<Transaction> loaded = await repository.LoadAsync();
    Console.WriteLine($"Visszatöltve {loaded.Count} tranzakció, azonos az eredetivel? {loaded.SequenceEqual(transactions)}");

    // direkt error, hogy lássuk az exceptiont
    await File.WriteAllTextAsync(filePath, "{ ez nem json ");
    await repository.LoadAsync();
}
catch (FinanceTrackerException ex)
{
    Console.WriteLine($"Hiba: {ex.Message}");
}
