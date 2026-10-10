using FinanceTracker.Core.Models;
using FinanceTracker.Core.Queries;

using NUnit.Framework;

namespace FinanceTracker.Tests.Queries;

[TestFixture]
public class TransactionQueryExtensionsTests
{
    private static readonly Transaction Salary1 = Transaction.Create(
        new DateOnly(2026, 1, 5), 450000m, TransactionType.Income, "Fizetés", "Januári bér");

    private static readonly Transaction Groceries1 = Transaction.Create(
        new DateOnly(2026, 1, 10), 12000m, TransactionType.Expense, "Étkezés", "Heti bevásárlás");

    private static readonly Transaction Groceries2 = Transaction.Create(
        new DateOnly(2026, 1, 20), 9000m, TransactionType.Expense, "étkezés", "Pékség");

    private static readonly Transaction Rent = Transaction.Create(
        new DateOnly(2026, 1, 25), 120000m, TransactionType.Expense, "Rezsi", "Albérlet");

    private static readonly Transaction Salary2 = Transaction.Create(
        new DateOnly(2026, 2, 5), 450000m, TransactionType.Income, "Fizetés", "Februári bér");

    private static readonly Transaction Cinema = Transaction.Create(
        new DateOnly(2026, 2, 14), 4500m, TransactionType.Expense, "Szórakozás", "Mozi");

    private static readonly Transaction Groceries3 = Transaction.Create(
        new DateOnly(2026, 2, 18), 15000m, TransactionType.Expense, "Étkezés", "Nagybevásárlás");

    // Dátum szerint rendezett adathalmaz, összes lekérdezés ezen keresztül.
    private static readonly List<Transaction> Dataset = new()
    {
        Salary1, Groceries1, Groceries2, Rent, Salary2, Cinema, Groceries3
    };

    // Szűrés

    [Test]
    public void InMonth_ReturnsOnlyTransactionsOfThatMonth()
    {
        List<Transaction> result = Dataset.InMonth(2026, 1).ToList();

        Assert.That(result, Is.EqualTo(new[] { Salary1, Groceries1, Groceries2, Rent }));
    }

    [Test]
    public void InMonth_NoMatch_ReturnsEmpty()
    {
        Assert.That(Dataset.InMonth(2025, 12), Is.Empty);
    }

    [Test]
    public void InPeriod_IsInclusiveOnBothEnds()
    {
        List<Transaction> result = Dataset
            .InPeriod(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 25))
            .ToList();

        Assert.That(result, Is.EqualTo(new[] { Groceries1, Groceries2, Rent }));
    }

    [Test]
    public void InPeriod_ReversedBounds_ReturnsEmpty()
    {
        Assert.That(Dataset.InPeriod(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)), Is.Empty);
    }

    [Test]
    public void OfCategory_IgnoresCaseAndPadding()
    {
        List<Transaction> result = Dataset.OfCategory("  ÉTKEZÉS ").ToList();

        Assert.That(result, Is.EqualTo(new[] { Groceries1, Groceries2, Groceries3 }));
    }

    [Test]
    public void ExpensesAndIncomes_SplitByType()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Dataset.Expenses().Count(), Is.EqualTo(5));
            Assert.That(Dataset.Incomes().ToList(), Is.EqualTo(new[] { Salary1, Salary2 }));
        });
    }

    [Test]
    public void SearchDescription_IsCaseInsensitiveAndAccentAware()
    {
        List<Transaction> result = Dataset.SearchDescription("BEVÁSÁRLÁS").ToList();

        Assert.That(result, Is.EqualTo(new[] { Groceries1, Groceries3 }));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void SearchDescription_BlankText_ReturnsEverything(string text)
    {
        Assert.That(Dataset.SearchDescription(text).Count(), Is.EqualTo(Dataset.Count));
    }

    [Test]
    public void Filters_CanBeChained()
    {
        List<Transaction> result = Dataset.InMonth(2026, 2).Expenses().TopExpenses(1).ToList();

        Assert.That(result, Is.EqualTo(new[] { Groceries3 }));
    }

    // Rendezés

    [Test]
    public void TopExpenses_ReturnsLargestExpensesFirst()
    {
        List<Transaction> result = Dataset.TopExpenses(2).ToList();

        Assert.That(result, Is.EqualTo(new[] { Rent, Groceries3 }));
    }

    [Test]
    public void TopExpenses_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Dataset.TopExpenses(-1));
    }

    [Test]
    public void TopExpenses_ZeroCount_ReturnsEmpty()
    {
        Assert.That(Dataset.TopExpenses(0), Is.Empty);
    }

    // Csoportosítás

    [Test]
    public void SummarizeByCategory_MergesCategoriesDifferingInCase_AndSortsByTotalDescending()
    {
        List<CategorySummary> result = Dataset.Expenses().SummarizeByCategory().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(new CategorySummary("Rezsi", 120000m, 1, 120000m)));
            Assert.That(result[1], Is.EqualTo(new CategorySummary("Étkezés", 36000m, 3, 12000m)));
            Assert.That(result[2], Is.EqualTo(new CategorySummary("Szórakozás", 4500m, 1, 4500m)));
        });
    }

    [Test]
    public void MonthlySummaries_GroupsByMonthInChronologicalOrder()
    {
        List<MonthlySummary> result = Dataset
            .OrderByDescending(t => t.Date)
            .MonthlySummaries()
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0], Is.EqualTo(new MonthlySummary(2026, 1, 450000m, 141000m)));
            Assert.That(result[1], Is.EqualTo(new MonthlySummary(2026, 2, 450000m, 19500m)));
            Assert.That(result[0].Balance, Is.EqualTo(309000m));
            Assert.That(result[1].Balance, Is.EqualTo(430500m));
        });
    }

    // Aggregáció

    [Test]
    public void Balance_IsIncomeMinusExpense()
    {
        Assert.That(Dataset.Balance(), Is.EqualTo(739500m));
    }

    [Test]
    public void Balance_EmptySequence_IsZero()
    {
        Assert.That(new List<Transaction>().Balance(), Is.EqualTo(0m));
    }

    [Test]
    public void AverageExpense_IsMeanOfExpensesOnly()
    {
        Assert.That(Dataset.AverageExpense(), Is.EqualTo(32100m));
    }

    [Test]
    public void AverageExpense_NoExpenses_IsZero()
    {
        Assert.That(Dataset.Incomes().AverageExpense(), Is.EqualTo(0m));
    }

    [Test]
    public void LargestAndSmallestExpense_ReturnExtremes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Dataset.LargestExpense(), Is.EqualTo(Rent));
            Assert.That(Dataset.SmallestExpense(), Is.EqualTo(Cinema));
        });
    }

    [Test]
    public void LargestAndSmallestExpense_NoExpenses_ReturnNull()
    {
        List<Transaction> incomesOnly = Dataset.Incomes().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(incomesOnly.LargestExpense(), Is.Null);
            Assert.That(incomesOnly.SmallestExpense(), Is.Null);
        });
    }

    [Test]
    public void LatestTransaction_ReturnsTheMostRecentOne()
    {
        Assert.That(Dataset.OrderByDescending(t => t.Amount).LatestTransaction(), Is.EqualTo(Groceries3));
    }

    [Test]
    public void LatestTransaction_EmptySequence_ReturnsNull()
    {
        Assert.That(new List<Transaction>().LatestTransaction(), Is.Null);
    }
}
