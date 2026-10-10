using FinanceTracker.Core.Exceptions;
using FinanceTracker.Core.Models;
using NUnit.Framework;

namespace FinanceTracker.Tests.Models;

[TestFixture]
public class TransactionTests
{
    private static readonly DateOnly SampleDate = new(2026, 3, 15);

    private static Transaction CreateSample(decimal amount = 1500m, string category = "Étkezés") =>
        Transaction.Create(SampleDate, amount, TransactionType.Expense, category, "Teszt tétel");

    // Létrehozás és validáció

    [Test]
    public void Create_ValidData_SetsAllProperties()
    {
        Transaction tx = Transaction.Create(SampleDate, 1500m, TransactionType.Expense, "Étkezés", "Ebéd");

        Assert.Multiple(() =>
        {
            Assert.That(tx.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(tx.Date, Is.EqualTo(SampleDate));
            Assert.That(tx.Amount, Is.EqualTo(1500m));
            Assert.That(tx.Type, Is.EqualTo(TransactionType.Expense));
            Assert.That(tx.Category, Is.EqualTo("Étkezés"));
            Assert.That(tx.Description, Is.EqualTo("Ebéd"));
        });
    }

    [Test]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        Transaction first = CreateSample();
        Transaction second = CreateSample();

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-5000)]
    public void Create_NonPositiveAmount_ThrowsInvalidTransactionException(int amount)
    {
        Assert.Throws<InvalidTransactionException>(() => CreateSample(amount: amount));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_BlankCategory_ThrowsInvalidTransactionException(string category)
    {
        Assert.Throws<InvalidTransactionException>(() => CreateSample(category: category));
    }

    [Test]
    public void Create_NullCategory_ThrowsInvalidTransactionException()
    {
        Assert.Throws<InvalidTransactionException>(() => CreateSample(category: null!));
    }

    [Test]
    public void Create_MinValueDate_ThrowsInvalidTransactionException()
    {
        Assert.Throws<InvalidTransactionException>(() =>
            Transaction.Create(DateOnly.MinValue, 100m, TransactionType.Income, "Fizetés"));
    }

    [Test]
    public void Create_UndefinedTransactionType_ThrowsInvalidTransactionException()
    {
        Assert.Throws<InvalidTransactionException>(() =>
            Transaction.Create(SampleDate, 100m, (TransactionType)99, "Fizetés"));
    }

    [Test]
    public void Create_PaddedCategory_IsTrimmed()
    {
        Transaction tx = CreateSample(category: "  Rezsi  ");

        Assert.That(tx.Category, Is.EqualTo("Rezsi"));
    }

    [Test]
    public void Create_NullDescription_BecomesEmptyString()
    {
        Transaction tx = Transaction.Create(SampleDate, 100m, TransactionType.Income, "Fizetés", null);

        Assert.That(tx.Description, Is.EqualTo(string.Empty));
    }

    // Immutabilitás és a "with" kifejezés

    [Test]
    public void With_ChangesOnlyTheGivenProperty_AndLeavesOriginalUntouched()
    {
        Transaction original = CreateSample(amount: 1500m);

        Transaction modified = original with { Amount = 2000m };

        Assert.Multiple(() =>
        {
            Assert.That(original.Amount, Is.EqualTo(1500m));
            Assert.That(modified.Amount, Is.EqualTo(2000m));
            Assert.That(modified.Id, Is.EqualTo(original.Id));
            Assert.That(modified.Category, Is.EqualTo(original.Category));
            Assert.That(ReferenceEquals(original, modified), Is.False);
        });
    }

    [Test]
    public void With_InvalidAmount_ThrowsInvalidTransactionException()
    {
        Transaction original = CreateSample();

        Assert.Throws<InvalidTransactionException>(() =>
        {
            _ = original with { Amount = -1m };
        });
    }

    // Egyenlőség (IEquatable<T>, record)

    [Test]
    public void Equals_CopyWithSameValues_AreEqual()
    {
        Transaction original = CreateSample();
        Transaction copy = original with { };

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.EqualTo(original));
            Assert.That(copy == original, Is.True);
            Assert.That(copy.GetHashCode(), Is.EqualTo(original.GetHashCode()));
        });
    }

    [Test]
    public void Equals_DifferentAmount_AreNotEqual()
    {
        Transaction original = CreateSample();
        Transaction modified = original with { Amount = original.Amount + 1 };

        Assert.That(modified, Is.Not.EqualTo(original));
    }

    [Test]
    public void Equals_SameDataButDifferentId_AreNotEqual()
    {
        Transaction first = CreateSample();
        Transaction second = CreateSample();

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void HashSet_EqualRecords_AreStoredOnce()
    {
        Transaction original = CreateSample();

        HashSet<Transaction> set = new() { original, original with { } };

        Assert.That(set, Has.Count.EqualTo(1));
    }

    // Rendezés (IComparable<T>)

    [Test]
    public void Sort_UsesDateAsNaturalOrder()
    {
        Transaction early = Transaction.Create(new DateOnly(2026, 1, 1), 100m, TransactionType.Income, "Fizetés");
        Transaction middle = Transaction.Create(new DateOnly(2026, 2, 1), 100m, TransactionType.Income, "Fizetés");
        Transaction late = Transaction.Create(new DateOnly(2026, 3, 1), 100m, TransactionType.Income, "Fizetés");
        List<Transaction> list = new() { late, early, middle };

        list.Sort();

        Assert.That(list, Is.EqualTo(new[] { early, middle, late }));
    }

    [Test]
    public void CompareTo_Null_ReturnsPositive()
    {
        Transaction tx = CreateSample();

        Assert.That(tx.CompareTo(null), Is.GreaterThan(0));
    }

    [Test]
    public void CompareTo_SameInstance_ReturnsZero()
    {
        Transaction tx = CreateSample();

        Assert.That(tx.CompareTo(tx), Is.EqualTo(0));
    }
}
