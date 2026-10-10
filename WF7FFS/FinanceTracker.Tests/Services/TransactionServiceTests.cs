using FinanceTracker.Core.Exceptions;
using FinanceTracker.Core.Models;
using FinanceTracker.Core.Services;
using FinanceTracker.Tests.Stubs;

using NUnit.Framework;

namespace FinanceTracker.Tests.Services;

[TestFixture]
public class TransactionServiceTests
{
    private InMemoryTransactionRepository _repository = null!;
    private TransactionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryTransactionRepository();
        _service = new TransactionService(_repository);
    }

    private static Transaction CreateExpense(int day = 1, decimal amount = 1000m)
    {
        return Transaction.Create(new DateOnly(2026, 3, day), amount, TransactionType.Expense, "Étkezés");
    }

    // Létrehozás

    [Test]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TransactionService(null!));
    }

    [Test]
    public void NewService_IsEmptyWithNothingToUndoAndNoUnsavedChanges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_service.Transactions, Is.Empty);
            Assert.That(_service.CanUndo, Is.False);
            Assert.That(_service.HasUnsavedChanges, Is.False);
        });
    }

    // Add

    [Test]
    public void Add_NewTransaction_IsStoredAndMarksUnsavedChanges()
    {
        Transaction transaction = CreateExpense();

        _service.Add(transaction);

        Assert.Multiple(() =>
        {
            Assert.That(_service.Transactions, Is.EqualTo(new[] { transaction }));
            Assert.That(_service.HasUnsavedChanges, Is.True);
            Assert.That(_service.CanUndo, Is.True);
        });
    }

    [Test]
    public void Add_NullTransaction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Add(null!));
    }

    [Test]
    public void Add_DuplicateId_ThrowsAndCreatesNoExtraUndoStep()
    {
        Transaction transaction = CreateExpense();
        _service.Add(transaction);

        Assert.Throws<InvalidTransactionException>(() => _service.Add(transaction));
        bool firstUndo = _service.Undo();
        bool secondUndo = _service.Undo();

        Assert.Multiple(() =>
        {
            Assert.That(firstUndo, Is.True);
            Assert.That(secondUndo, Is.False);
            Assert.That(_service.Transactions, Is.Empty);
        });
    }

    // Lekérdezés 

    [Test]
    public void Transactions_AreOrderedByDateRegardlessOfInsertionOrder()
    {
        Transaction late = CreateExpense(day: 20);
        Transaction early = CreateExpense(day: 2);
        Transaction middle = CreateExpense(day: 10);

        _service.Add(late);
        _service.Add(early);
        _service.Add(middle);

        Assert.That(_service.Transactions, Is.EqualTo(new[] { early, middle, late }));
    }

    [Test]
    public void Transactions_ReturnsSnapshotUnaffectedByLaterChanges()
    {
        _service.Add(CreateExpense(day: 1));
        IReadOnlyList<Transaction> snapshot = _service.Transactions;

        _service.Add(CreateExpense(day: 2));

        Assert.That(snapshot, Has.Count.EqualTo(1));
    }

    [Test]
    public void Find_ExistingId_ReturnsTransaction()
    {
        Transaction transaction = CreateExpense();
        _service.Add(transaction);

        Transaction? found = _service.Find(transaction.Id);

        Assert.That(found, Is.EqualTo(transaction));
    }

    [Test]
    public void Find_UnknownId_ReturnsNull()
    {
        Assert.That(_service.Find(Guid.NewGuid()), Is.Null);
    }

    // Update

    [Test]
    public void Update_ExistingTransaction_ReplacesItKeepingTheSameId()
    {
        Transaction original = CreateExpense(amount: 1000m);
        _service.Add(original);
        Transaction modified = original with { Amount = 2500m };

        _service.Update(modified);

        Assert.Multiple(() =>
        {
            Assert.That(_service.Transactions, Has.Count.EqualTo(1));
            Assert.That(_service.Find(original.Id)!.Amount, Is.EqualTo(2500m));
        });
    }

    [Test]
    public void Update_UnknownId_ThrowsTransactionNotFoundExceptionWithTheId()
    {
        Transaction stranger = CreateExpense();

        TransactionNotFoundException ex =
            Assert.Throws<TransactionNotFoundException>(() => _service.Update(stranger));

        Assert.That(ex.TransactionId, Is.EqualTo(stranger.Id));
    }

    // Remove

    [Test]
    public void Remove_ExistingId_RemovesTransaction()
    {
        Transaction transaction = CreateExpense();
        _service.Add(transaction);

        _service.Remove(transaction.Id);

        Assert.That(_service.Transactions, Is.Empty);
    }

    [Test]
    public void Remove_UnknownId_ThrowsTransactionNotFoundException()
    {
        Assert.Throws<TransactionNotFoundException>(() => _service.Remove(Guid.NewGuid()));
    }

    // Undo

    [Test]
    public void Undo_AfterAdd_RemovesTheTransaction()
    {
        _service.Add(CreateExpense());

        bool undone = _service.Undo();

        Assert.Multiple(() =>
        {
            Assert.That(undone, Is.True);
            Assert.That(_service.Transactions, Is.Empty);
        });
    }

    [Test]
    public void Undo_AfterUpdate_RestoresThePreviousVersion()
    {
        Transaction original = CreateExpense(amount: 1000m);
        _service.Add(original);
        _service.Update(original with { Amount = 2500m });

        _service.Undo();

        Assert.That(_service.Find(original.Id), Is.EqualTo(original));
    }

    [Test]
    public void Undo_AfterRemove_RestoresTheTransaction()
    {
        Transaction transaction = CreateExpense();
        _service.Add(transaction);
        _service.Remove(transaction.Id);

        _service.Undo();

        Assert.That(_service.Transactions, Is.EqualTo(new[] { transaction }));
    }

    [Test]
    public void Undo_MultipleSteps_RevertsInReverseOrder()
    {
        Transaction first = CreateExpense(day: 1);
        Transaction second = CreateExpense(day: 2);
        _service.Add(first);
        _service.Add(second);

        _service.Undo();
        IReadOnlyList<Transaction> afterFirstUndo = _service.Transactions;
        _service.Undo();

        Assert.Multiple(() =>
        {
            Assert.That(afterFirstUndo, Is.EqualTo(new[] { first }));
            Assert.That(_service.Transactions, Is.Empty);
            Assert.That(_service.CanUndo, Is.False);
        });
    }

    [Test]
    public void Undo_NothingToUndo_ReturnsFalse()
    {
        Assert.That(_service.Undo(), Is.False);
    }

    [Test]
    public async Task Undo_AfterSave_MarksUnsavedChangesAgain()
    {
        _service.Add(CreateExpense());
        await _service.SaveAsync();

        _service.Undo();

        Assert.That(_service.HasUnsavedChanges, Is.True);
    }

    // Betöltés

    [Test]
    public async Task LoadAsync_ReplacesContentAndResetsUndoAndUnsavedFlag()
    {
        _service.Add(CreateExpense(day: 1));
        Transaction stored = CreateExpense(day: 5);
        _repository.Stored.Add(stored);

        await _service.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_service.Transactions, Is.EqualTo(new[] { stored }));
            Assert.That(_service.CanUndo, Is.False);
            Assert.That(_service.HasUnsavedChanges, Is.False);
            Assert.That(_repository.LoadCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void LoadAsync_RepositoryFails_KeepsExistingStateAndPropagatesError()
    {
        Transaction existing = CreateExpense();
        _service.Add(existing);
        _repository.ExceptionToThrow = new DataAccessException("Sérült fájl.");

        Assert.ThrowsAsync<DataAccessException>(async () => await _service.LoadAsync());

        Assert.Multiple(() =>
        {
            Assert.That(_service.Transactions, Is.EqualTo(new[] { existing }));
            Assert.That(_service.HasUnsavedChanges, Is.True);
        });
    }

    [Test]
    public void LoadAsync_DuplicateIdsInStoredData_ThrowsDataAccessException()
    {
        Transaction transaction = CreateExpense();
        _repository.Stored.Add(transaction);
        _repository.Stored.Add(transaction);

        Assert.ThrowsAsync<DataAccessException>(async () => await _service.LoadAsync());
    }

    // Mentés

    [Test]
    public async Task SaveAsync_PersistsAllTransactionsAndClearsUnsavedFlag()
    {
        Transaction first = CreateExpense(day: 1);
        Transaction second = CreateExpense(day: 2);
        _service.Add(second);
        _service.Add(first);

        await _service.SaveAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_repository.Stored, Is.EqualTo(new[] { first, second }));
            Assert.That(_repository.SaveCallCount, Is.EqualTo(1));
            Assert.That(_service.HasUnsavedChanges, Is.False);
        });
    }

    [Test]
    public void SaveAsync_RepositoryFails_KeepsUnsavedFlag()
    {
        _service.Add(CreateExpense());
        _repository.ExceptionToThrow = new DataAccessException("Nincs jogosultság.");

        Assert.ThrowsAsync<DataAccessException>(async () => await _service.SaveAsync());

        Assert.That(_service.HasUnsavedChanges, Is.True);
    }
}
