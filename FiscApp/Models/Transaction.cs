namespace FiscApp.Models;

public enum TransactionType
{
    Income,
    Expense
}

public class Transaction
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public BudgetCategory? Category { get; set; }
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
