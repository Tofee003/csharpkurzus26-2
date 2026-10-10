using FinanceTracker.Core.Exceptions;

namespace FinanceTracker.Core.Models;

// Tranzakció builder

public sealed class TransactionBuilder
{
    private DateOnly? _date;
    private decimal? _amount;
    private TransactionType? _type;
    private string? _category;
    private string _description = string.Empty;

    public TransactionBuilder OnDate(DateOnly date)
    {
        _date = date;
        return this;
    }

    public TransactionBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public TransactionBuilder AsIncome()
    {
        return OfType(TransactionType.Income);
    }

    public TransactionBuilder AsExpense()
    {
        return OfType(TransactionType.Expense);
    }

    public TransactionBuilder OfType(TransactionType type)
    {
        _type = type;
        return this;
    }

    public TransactionBuilder InCategory(string category)
    {
        _category = category;
        return this;
    }

    public TransactionBuilder Described(string description)
    {
        _description = description;
        return this;
    }

    // Létrehozza a tranzakciót, hiányzó vagy érvénytelen adatnál "InvalidTransactionException" dob.
    public Transaction Build()
    {
        List<string> missing = new();

        if (_date is null) missing.Add("dátum");
        if (_amount is null) missing.Add("összeg");
        if (_type is null) missing.Add("típus");
        if (_category is null) missing.Add("kategória");

        if (missing.Count > 0)
        {
            throw new InvalidTransactionException(
                $"A tranzakció nem hozható létre, hiányzó adat(ok): {string.Join(", ", missing)}.");
        }

        return Transaction.Create(_date!.Value, _amount!.Value, _type!.Value, _category!, _description);
    }
}
