using FinanceTracker.Core.Exceptions;
using FinanceTracker.Core.Models;

// Dummy adat
Console.OutputEncoding = System.Text.Encoding.UTF8;

try
{
    Transaction lunch = new TransactionBuilder()
        .OnDate(new DateOnly(2026, 10, 9))
        .WithAmount(2490m)
        .AsExpense()
        .InCategory("Étkezés")
        .Described("Ebéd az egyetemen")
        .Build();

    Console.WriteLine(lunch);

    Transaction corrected = lunch with { Amount = 2690m };
    Console.WriteLine(corrected);
    Console.WriteLine($"Az eredeti összeg változatlan: {lunch.Amount}; azonos a két példány? {lunch == corrected}");

    // validacio
    _ = lunch with { Amount = -5m };
}
catch (InvalidTransactionException ex)
{
    Console.WriteLine($"Hiba: {ex.Message}");
}
