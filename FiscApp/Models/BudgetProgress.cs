using FiscApp.Services;
using Microsoft.Maui.Graphics;

namespace FiscApp.Models;

/// <summary>
/// A read-only snapshot of how much a single BudgetCategory has been spent against, FOR ONE
/// SPECIFIC MONTH. This replaces the old design where BudgetCategory.AmountSpent was a running
/// total that transactions incremented/decremented directly — that approach had no concept of
/// "this month" vs "last month" and just kept accumulating forever.
///
/// Instead, BudgetViewModel and ReportsViewModel build a fresh list of these on demand by asking
/// FinanceDataStore.GetSpentAmount(category, month) to sum the actual Transactions for that
/// category and month. Nothing here is ever mutated after construction — a new BudgetProgress
/// is built every time the underlying transactions change (see BudgetViewModel.RefreshProgress),
/// the same "recompute from scratch" pattern already used for the Reports charts.
/// </summary>
public class BudgetProgress
{
    public required BudgetCategory Category { get; init; }
    public required decimal AmountSpent { get; init; }

    public string Name => Category.Name;
    public decimal MonthlyLimit => Category.MonthlyLimit;

    // Fraction of the limit used (1.0 = 100%). Can exceed 1.0 when over budget.
    public double PercentageUsed =>
        MonthlyLimit <= 0 ? 0 : (double)(AmountSpent / MonthlyLimit);

    /// <summary>
    /// Green under 80% used, orange from 80-99% ("approaching limit"), red at 100%+ (over budget).
    /// Colors come from the app's STATIC resources (SuccessColor / WarningColor / DangerColor in
    /// Resources/Styles/Colors.xaml) so the bars use the same palette as the rest of the app.
    /// </summary>
    public Color StatusColor =>
        PercentageUsed >= 1.0 ? ThemeService.GetColor("DangerColor", Colors.Red)
        : PercentageUsed >= 0.8 ? ThemeService.GetColor("WarningColor", Colors.Orange)
        : ThemeService.GetColor("SuccessColor", Colors.Green);
}
