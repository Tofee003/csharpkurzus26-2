using FinanceTracker.Core.Models;
using FinanceTracker.Core.Persistence;

namespace FinanceTracker.Tests.Stubs;


// fájlrendszer nélkül memóriában tárol, így a service tesztjei gyorsak.
public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    public List<Transaction> Stored { get; } = new();

    public int LoadCallCount { get; private set; }

    public int SaveCallCount { get; private set; }

    // Ha be van állítva, minden betöltés és mentés ezt a kivételt dobja.
    public Exception? ExceptionToThrow { get; set; }

    public Task<IReadOnlyList<Transaction>> LoadAsync(CancellationToken cancellationToken = default)
    {
        LoadCallCount++;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult<IReadOnlyList<Transaction>>(Stored.ToList());
    }

    public Task SaveAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        SaveCallCount++;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        List<Transaction> copy = transactions.ToList();
        Stored.Clear();
        Stored.AddRange(copy);
        return Task.CompletedTask;
    }
}
