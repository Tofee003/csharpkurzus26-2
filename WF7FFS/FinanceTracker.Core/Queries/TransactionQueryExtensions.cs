using FinanceTracker.Core.Comparers;
using FinanceTracker.Core.Models;

namespace FinanceTracker.Core.Queries;


// Láncolható lekérdezések (LINQ)
// pl.: transactions.InMonth(2026, 3).Expenses().TopExpenses(5)
// Az állapotot a "TransactionService" kezeli, a lekérdezések állapotmentes, tiszta függvények.
// Az eredmény csak bejáráskor számolódik (IEnumerable)
public static class TransactionQueryExtensions
{
    // Szűrés (Where)

    public static IEnumerable<Transaction> InMonth(this IEnumerable<Transaction> transactions, int year, int month)
    {
        return transactions.Where(t => t.Date.Year == year && t.Date.Month == month);
    }

    // Zárt intervallum: két határnap benne. Fordított határoknál üres eredmény
    public static IEnumerable<Transaction> InPeriod(this IEnumerable<Transaction> transactions, DateOnly from, DateOnly to)
    {
        return transactions.Where(t => t.Date >= from && t.Date <= to);
    }

    public static IEnumerable<Transaction> OfCategory(this IEnumerable<Transaction> transactions, string category)
    {
        return transactions.Where(t => CategoryComparer.Instance.Equals(t.Category, category));
    }

    public static IEnumerable<Transaction> Expenses(this IEnumerable<Transaction> transactions)
    {
        return transactions.Where(t => t.Type == TransactionType.Expense);
    }

    public static IEnumerable<Transaction> Incomes(this IEnumerable<Transaction> transactions)
    {
        return transactions.Where(t => t.Type == TransactionType.Income);
    }

    // Kis és nagybetűre insensitive keresés a leírásban; üres szövegnél nem szűr.
    public static IEnumerable<Transaction> SearchDescription(this IEnumerable<Transaction> transactions, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return transactions;
        }

        string term = text.Trim();
        return transactions.Where(t => t.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    // Rendezés (OrderByDescending)

    // A legnagyobb kiadások csökkenő sorrendben, azonos értéknél a korábbi dátum prioritásban
    public static IEnumerable<Transaction> TopExpenses(this IEnumerable<Transaction> transactions, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return transactions
            .Expenses()
            .OrderByDescending(t => t.Amount)
            .ThenBy(t => t.Date)
            .Take(count);
    }

    // Csoportosítás (GroupBy)

    // Kategóriánkénti összesítés, csökkenő összeg szerint.
    public static IEnumerable<CategorySummary> SummarizeByCategory(this IEnumerable<Transaction> transactions)
    {
        return transactions
            .GroupBy(t => t.Category, CategoryComparer.Instance)
            .Select(g => new CategorySummary(g.Key, g.Sum(t => t.Amount), g.Count(), g.Average(t => t.Amount)))
            .OrderByDescending(summary => summary.Total);
    }

    // Havi összesítés időrendben.
    public static IEnumerable<MonthlySummary> MonthlySummaries(this IEnumerable<Transaction> transactions)
    {
        return from t in transactions
               group t by new { t.Date.Year, t.Date.Month } into month
               orderby month.Key.Year, month.Key.Month
               select new MonthlySummary(
                   month.Key.Year,
                   month.Key.Month,
                   month.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                   month.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount));
    }

    // Aggregáció (Sum, Average, MaxBy, MinBy, FirstOrDefault)

    // Bevételek és kiadások különbsége, üres bemenetnél 0
    public static decimal Balance(this IEnumerable<Transaction> transactions)
    {
        return transactions.Sum(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);
    }

    // Kiadások átlaga, ha nincs 0, mert üres soron exception keletkezne
    public static decimal AverageExpense(this IEnumerable<Transaction> transactions)
    {
        return transactions.Expenses().Select(t => t.Amount).DefaultIfEmpty(0m).Average();
    }

    public static Transaction? LargestExpense(this IEnumerable<Transaction> transactions)
    {
        return transactions.Expenses().MaxBy(t => t.Amount);
    }

    public static Transaction? SmallestExpense(this IEnumerable<Transaction> transactions)
    {
        return transactions.Expenses().MinBy(t => t.Amount);
    }

    public static Transaction? LatestTransaction(this IEnumerable<Transaction> transactions)
    {
        return transactions.OrderByDescending(t => t.Date).FirstOrDefault();
    }
}
