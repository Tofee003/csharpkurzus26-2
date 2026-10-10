using FinanceTracker.Core.Exceptions;

namespace FinanceTracker.Core.Models;

// Egy pénzügyi tranzakció. Immutable (record + init-only tulajdonságok): módosítani csak a "with" kifejezéssel lehet, ez egy új példányt hoz létre.
// A "required" kulcsszó miatt nem lehet félkész objektumot létrehozni.
public sealed record Transaction : IComparable<Transaction>
{
    private readonly Guid _id;
    private readonly DateOnly _date;
    private readonly decimal _amount;
    private readonly TransactionType _type;
    private readonly string _category = string.Empty;
    private readonly string _description = string.Empty;

    public required Guid Id
    {
        get => _id;
        init => _id = value != Guid.Empty
            ? value
            : throw new InvalidTransactionException("A tranzakció azonosítója nem lehet üres.");
    }

    public required DateOnly Date
    {
        get => _date;
        init => _date = value != DateOnly.MinValue
            ? value
            : throw new InvalidTransactionException("A tranzakció dátuma nincs megadva.");
    }

    public required decimal Amount
    {
        get => _amount;
        init => _amount = value > 0
            ? value
            : throw new InvalidTransactionException("Az összegnek pozitívnak kell lennie.");
    }

    public required TransactionType Type
    {
        get => _type;
        init => _type = Enum.IsDefined(value)
            ? value
            : throw new InvalidTransactionException("Ismeretlen tranzakciótípus.");
    }

    public required string Category
    {
        get => _category;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidTransactionException("A kategória megadása kötelező.");
            }

            _category = value.Trim();
        }
    }

    // Opcionális desc; null esetén üres szöveg
    public string Description
    {
        get => _description;
        init => _description = value?.Trim() ?? string.Empty;
    }

    // Factory metódus: új tranzakciót hoz létre, és generál hozzá azonosítót.
    public static Transaction Create(
        DateOnly date,
        decimal amount,
        TransactionType type,
        string category,
        string? description = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            Date = date,
            Amount = amount,
            Type = type,
            Category = category,
            Description = description ?? string.Empty
        };
    }

    // Természetes sorrend: dátum szerint, azonos napon az azonosító alapján.
    // Az Equals() == true esetén a CompareTo 0, fordítva nem fix.
    public int CompareTo(Transaction? other)
    {
        if (other is null)
        {
            return 1;
        }

        int byDate = Date.CompareTo(other.Date);
        return byDate != 0 ? byDate : Id.CompareTo(other.Id);
    }
}
