namespace FiscApp.Models;

/// <summary>Whether a transaction is money coming in or money going out.</summary>
public enum TransactionType
{
    Income,
    Expense
}

/// <summary>
/// A single logged income or expense entry. Expense transactions are optionally linked to a
/// BudgetCategory (via the Category property) so that adding, editing, or deleting one can
/// automatically keep that category's AmountSpent in sync. Income transactions don't affect
/// any budget category, since a budget only tracks spending.
/// </summary>
public class Transaction
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;

    // Only set for Expense transactions. Kept so that editing or deleting this transaction later
    // can reverse its effect on the correct category's AmountSpent (see MainPage.xaml.cs).
    public BudgetCategory? Category { get; set; }

    // Optional free-text notes the user can attach to a transaction, separate from Description.
    public string? Notes { get; set; }

    // Convenience properties for binding in the UI
    public string DisplayAmount =>
        Type == TransactionType.Expense
            ? $"-${Amount:N2}"
            : $"+${Amount:N2}";

    public Color AmountColor =>
        Type == TransactionType.Expense
            ? Color.FromArgb("#D64545")
            : Color.FromArgb("#2E9E5B");
}
