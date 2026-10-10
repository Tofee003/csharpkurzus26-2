namespace FinanceTracker.Core.Exceptions;

// Saját exceptionök közös őse
public class FinanceTrackerException : Exception
{
    public FinanceTrackerException(string message) : base(message)
    {
    }

    public FinanceTrackerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
