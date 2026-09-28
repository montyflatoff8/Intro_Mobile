using FiscApp.Services;

namespace FiscApp.Models;

/// <summary>Whether a transaction is money coming in or money going out.</summary>
public enum TransactionType
{
    Income,
    Expense
}

/// <summary>
/// A single logged income or expense entry. Expense transactions are linked to a
/// BudgetCategory (via the Category property) so FinanceDataStore.GetSpentAmount can total
/// spending per category per month. Income transactions have no category, since a budget
/// only tracks spending.
/// </summary>
public class Transaction
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;

    // Only set for Expense transactions. GetSpentAmount matches on this reference to decide
    // which category's monthly total this transaction counts toward.
    public BudgetCategory? Category { get; set; }

    // Optional free-text notes the user can attach to a transaction, separate from Description.
    public string? Notes { get; set; }

    // Convenience properties for binding in the UI
    // "-$12.50" for expenses, "+$12.50" for income.
    public string DisplayAmount =>
        Type == TransactionType.Expense
            ? $"-${Amount:N2}"
            : $"+${Amount:N2}";

    // Red for expenses, green for income. Pulled from the app's STATIC resources
    // (DangerColor / SuccessColor in Resources/Styles/Colors.xaml) via ThemeService.GetColor,
    // so the amount text always matches the Delete button, budget bars, etc. The second
    // argument is only a fallback in case the resource dictionary isn't loaded.
    public Color AmountColor =>
        Type == TransactionType.Expense
            ? ThemeService.GetColor("DangerColor", Colors.Red)
            : ThemeService.GetColor("SuccessColor", Colors.Green);

    // Category label shown under the description on each transaction row.
    public string CategoryDisplay =>
        Type == TransactionType.Income
            ? "Income"
            : Category?.Name ?? "Uncategorized";
}