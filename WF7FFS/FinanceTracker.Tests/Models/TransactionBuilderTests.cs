using FinanceTracker.Core.Exceptions;
using FinanceTracker.Core.Models;
using NUnit.Framework;

namespace FinanceTracker.Tests.Models;

[TestFixture]
public class TransactionBuilderTests
{
    private static TransactionBuilder CompleteBuilder() =>
        new TransactionBuilder()
            .OnDate(new DateOnly(2026, 10, 9))
            .WithAmount(2490m)
            .AsExpense()
            .InCategory("Étkezés");

    [Test]
    public void Build_AllRequiredPartsSet_ReturnsMatchingTransaction()
    {
        Transaction tx = CompleteBuilder().Described("Ebéd").Build();

        Assert.Multiple(() =>
        {
            Assert.That(tx.Date, Is.EqualTo(new DateOnly(2026, 10, 9)));
            Assert.That(tx.Amount, Is.EqualTo(2490m));
            Assert.That(tx.Type, Is.EqualTo(TransactionType.Expense));
            Assert.That(tx.Category, Is.EqualTo("Étkezés"));
            Assert.That(tx.Description, Is.EqualTo("Ebéd"));
            Assert.That(tx.Id, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public void Build_WithoutDescription_UsesEmptyDescription()
    {
        Transaction tx = CompleteBuilder().Build();

        Assert.That(tx.Description, Is.EqualTo(string.Empty));
    }

    [Test]
    public void AsIncome_SetsIncomeType()
    {
        Transaction tx = CompleteBuilder().AsIncome().Build();

        Assert.That(tx.Type, Is.EqualTo(TransactionType.Income));
    }

    [Test]
    public void Build_NothingSet_ThrowsAndListsEveryMissingPart()
    {
        InvalidTransactionException ex = Assert.Throws<InvalidTransactionException>(() => new TransactionBuilder().Build());

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Contain("dátum"));
            Assert.That(ex.Message, Does.Contain("összeg"));
            Assert.That(ex.Message, Does.Contain("típus"));
            Assert.That(ex.Message, Does.Contain("kategória"));
        });
    }

    [Test]
    public void Build_MissingAmountOnly_ThrowsInvalidTransactionException()
    {
        TransactionBuilder builder = new TransactionBuilder()
            .OnDate(new DateOnly(2026, 10, 9))
            .AsExpense()
            .InCategory("Étkezés");

        Assert.Throws<InvalidTransactionException>(() => builder.Build());
    }

    [Test]
    public void Build_InvalidAmount_ThrowsInvalidTransactionException()
    {
        TransactionBuilder builder = CompleteBuilder().WithAmount(-100m);

        Assert.Throws<InvalidTransactionException>(() => builder.Build());
    }

    [Test]
    public void Build_CalledTwice_ProducesTransactionsWithDifferentIds()
    {
        TransactionBuilder builder = CompleteBuilder();

        Transaction first = builder.Build();
        Transaction second = builder.Build();

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }
}
