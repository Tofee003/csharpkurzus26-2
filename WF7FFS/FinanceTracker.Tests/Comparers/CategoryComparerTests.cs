using FinanceTracker.Core.Comparers;
using FinanceTracker.Core.Models;
using NUnit.Framework;

namespace FinanceTracker.Tests.Comparers;

[TestFixture]
public class CategoryComparerTests
{
    private static readonly CategoryComparer Comparer = CategoryComparer.Instance;

    [TestCase("Étkezés", "étkezés")]
    [TestCase("Rezsi", "  REZSI ")]
    [TestCase("Szórakozás", "Szórakozás")]
    public void Equals_SameCategoryIgnoringCaseAndPadding_ReturnsTrueWithEqualHashCodes(string a, string b)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Comparer.Equals(a, b), Is.True);
            Assert.That(Comparer.GetHashCode(a), Is.EqualTo(Comparer.GetHashCode(b)));
        });
    }

    [Test]
    public void Equals_DifferentCategories_ReturnsFalse()
    {
        Assert.That(Comparer.Equals("Rezsi", "Étkezés"), Is.False);
    }

    [Test]
    public void Equals_NullAndValue_ReturnsFalse()
    {
        Assert.That(Comparer.Equals(null, "Rezsi"), Is.False);
    }

    [Test]
    public void GetHashCode_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Comparer.GetHashCode(null!));
    }

    [Test]
    public void GroupBy_WithComparer_MergesCategoriesDifferingOnlyInCase()
    {
        DateOnly date = new(2026, 3, 15);
        List<Transaction> transactions = new()
        {
            Transaction.Create(date, 1000m, TransactionType.Expense, "Étkezés"),
            Transaction.Create(date, 2000m, TransactionType.Expense, "étkezés"),
            Transaction.Create(date, 5000m, TransactionType.Expense, "Rezsi")
        };

        List<IGrouping<string, Transaction>> groups = transactions
            .GroupBy(t => t.Category, CategoryComparer.Instance)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(groups, Has.Count.EqualTo(2));
            Assert.That(groups.Single(g => Comparer.Equals(g.Key, "Étkezés")).Sum(t => t.Amount),
                Is.EqualTo(3000m));
        });
    }
}
