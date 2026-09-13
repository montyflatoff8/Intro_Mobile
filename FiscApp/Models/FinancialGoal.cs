namespace FiscApp.Models;

/// <summary>
/// A savings goal the user is tracking toward (e.g. "Emergency Fund", target $3,000).
/// Purely informational right now — nothing automatically adds to CurrentAmount the way
/// transactions update BudgetCategory.AmountSpent.
/// </summary>
public class FinancialGoal
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }

    // Convenience properties for binding in the UI
    public double Progress =>
        TargetAmount <= 0 ? 0 : (double)(CurrentAmount / TargetAmount);

    public string ProgressText =>
        $"${CurrentAmount:N0} of ${TargetAmount:N0}";

    public string RemainingText =>
        $"${Math.Max(TargetAmount - CurrentAmount, 0):N0} to go";
}
