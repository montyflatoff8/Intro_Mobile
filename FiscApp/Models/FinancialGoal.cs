namespace FiscApp.Models;

/// <summary>
/// A savings goal the user is tracking toward (e.g. "Emergency Fund", target $3,000).
/// Purely informational right now — CurrentAmount is only set in sample data; no transaction
/// or UI action adds to it yet.
/// </summary>
public class FinancialGoal
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }

    // Convenience properties for binding in the UI
    // 0.0–1.0 fraction for the ProgressBar; guards against divide-by-zero.
    public double Progress =>
        TargetAmount <= 0 ? 0 : (double)(CurrentAmount / TargetAmount);

    public string ProgressText =>
        $"${CurrentAmount:N0} of ${TargetAmount:N0}";

    public string RemainingText =>
        $"${Math.Max(TargetAmount - CurrentAmount, 0):N0} to go";
}
