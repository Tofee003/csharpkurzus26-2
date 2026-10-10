using FinanceTracker.Core.Models;

namespace FinanceTracker.Core.Persistence;

// Tranzakció tartós tár.
// Service ezt az interfészt ismeri, így a tárolás típusa cserélhető
public interface ITransactionRepository
{
    // Összes tranzakció betöltése, ha nincs üres lista.
    // "Exceptions.DataAccessException" - A betöltés nem sikerült - szöveggel.
    Task<IReadOnlyList<Transaction>> LoadAsync(CancellationToken cancellationToken = default);

    // Megadott tranzakciók mentése felülírással.
    // "Exceptions.DataAccessException" - A mentés nem sikerült - szöveggel.
    Task SaveAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
}
