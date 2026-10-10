using FinanceTracker.Core.Exceptions;
using FinanceTracker.Core.Models;
using FinanceTracker.Core.Persistence;

namespace FinanceTracker.Core.Services;

// A tranzakciók állapotát kezeli: hozzáadás, módosítás, törlés, visszavonás, betöltés, mentés.
public sealed class TransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly Stack<Action> _undoStack = new();
    private Dictionary<Guid, Transaction> _index = new();

    public TransactionService(ITransactionRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    // Az aktuális tranzakciók dátum szerint rendezve
    public IReadOnlyList<Transaction> Transactions
    {
        get
        {
            // Az Order() a Transaction IComparable<T> implementációt használja.
            return _index.Values.Order().ToList();
        }
    }

    // True, ha az utolsó mentés vagy betöltés óta történt változás.
    public bool HasUnsavedChanges { get; private set; }

    public bool CanUndo => _undoStack.Count > 0;

    public Transaction? Find(Guid id)
    {
        return _index.GetValueOrDefault(id);
    }

    public void Add(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (!_index.TryAdd(transaction.Id, transaction))
        {
            throw new InvalidTransactionException("Már létezik tranzakció ezzel az azonosítóval.");
        }

        _undoStack.Push(() => _index.Remove(transaction.Id));
        HasUnsavedChanges = true;
    }

    public void Update(Transaction updated)
    {
        ArgumentNullException.ThrowIfNull(updated);

        if (!_index.TryGetValue(updated.Id, out Transaction? found))
        {
            throw new TransactionNotFoundException(updated.Id);
        }

        Transaction previous = found;
        _index[updated.Id] = updated;
        _undoStack.Push(() => _index[previous.Id] = previous);
        HasUnsavedChanges = true;
    }

    public void Remove(Guid id)
    {
        if (!_index.Remove(id, out Transaction? removed))
        {
            throw new TransactionNotFoundException(id);
        }

        Transaction original = removed;
        _undoStack.Push(() => _index[original.Id] = original);
        HasUnsavedChanges = true;
    }

    // Visszavonja az utolsó műveletet (false, ha nincs mit visszavonni).
    public bool Undo()
    {
        if (!_undoStack.TryPop(out Action? undoAction))
        {
            return false;
        }

        undoAction();
        HasUnsavedChanges = true;
        return true;
    }

    // Betölti a mentett adatokat, majd felülírja velük az aktuális állapotot.
    // Hiba esetén a meglévő állapot változatlan marad.
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Transaction> loaded = await _repository.LoadAsync(cancellationToken);

        Dictionary<Guid, Transaction> index = new();
        foreach (Transaction transaction in loaded)
        {
            if (!index.TryAdd(transaction.Id, transaction))
            {
                throw new DataAccessException(
                    "A betöltött adatok ugyanazzal az azonosítóval több tranzakciót is tartalmaznak.");
            }
        }

        _index = index;
        _undoStack.Clear();
        HasUnsavedChanges = false;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _repository.SaveAsync(Transactions, cancellationToken);
        HasUnsavedChanges = false;
    }
}
