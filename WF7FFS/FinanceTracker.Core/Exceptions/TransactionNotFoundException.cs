namespace FinanceTracker.Core.Exceptions;

// Nem létező tranzakció módosítása vagy törlése dobja.
public sealed class TransactionNotFoundException : FinanceTrackerException
{
    public TransactionNotFoundException(Guid transactionId)
        : base($"Nem található tranzakció a(z) {transactionId} azonosítóval.")
    {
        TransactionId = transactionId;
    }

    public Guid TransactionId { get; }
}
