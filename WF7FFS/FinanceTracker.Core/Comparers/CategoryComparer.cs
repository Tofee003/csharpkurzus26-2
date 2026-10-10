namespace FinanceTracker.Core.Comparers;

// Kategória nevek összehasonlítása (kis és nagybetű, illetve input eleji / végi space insensitive (trim))
// IEqualityComparer, mert felhasználás helyétől függ
// (GroupBy, Dictionary, Distinct)

public sealed class CategoryComparer : IEqualityComparer<string>
{
    public static readonly CategoryComparer Instance = new();

    public bool Equals(string? x, string? y)
    {
        return string.Equals(x?.Trim(), y?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(string obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Trim());
    }
}
