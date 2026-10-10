namespace FinanceTracker.Core.Models;

// Immutable - Egy kategória összesítése
public sealed record CategorySummary(string Category, decimal Total, int Count, decimal Average);
