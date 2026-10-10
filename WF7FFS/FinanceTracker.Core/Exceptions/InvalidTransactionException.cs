namespace FinanceTracker.Core.Exceptions;

// Ha egy tranzakció megsért egy domain szabályt (pl.: nem pozitív összeg, üres)

public sealed class InvalidTransactionException : FinanceTrackerException
{
    public InvalidTransactionException(string message) : base(message)
    {
    }

    public InvalidTransactionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
