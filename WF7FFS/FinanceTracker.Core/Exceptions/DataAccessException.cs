namespace FinanceTracker.Core.Exceptions;

// Akkor keletkezik, ha az adatok betöltése vagy mentése sikertelen.
// pl.: hiányzó role, sérült fájl, hibás JSON.
// Az eredeti exception az InnerException-ben marad.
public sealed class DataAccessException : FinanceTrackerException
{
    public DataAccessException(string message) : base(message)
    {
    }

    public DataAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
