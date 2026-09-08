namespace FiscApp.Models;

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
