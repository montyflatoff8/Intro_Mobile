using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FiscApp.Models;
using FiscApp.Services;

namespace FiscApp.ViewModels;

/// <summary>
/// Backs the Budget Setup page. Lets the user create categories with a monthly limit, and shows
/// each category's progress for a SELECTED MONTH (defaulting to the current month, with
/// Previous/Next commands to look at other months) — the "genuinely monthly" calculation the
/// assignment feedback asked for. CategoryProgress is rebuilt from scratch by RefreshProgress()
/// any time a transaction or category changes, the same pattern ReportsViewModel already used
/// for its charts.
/// </summary>
public partial class BudgetViewModel : BaseViewModel
{
    private readonly FinanceDataStore store;

    [ObservableProperty]
    private string newCategoryName = string.Empty;

    [ObservableProperty]
    private string newCategoryLimitText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthLabel))]
    private DateTime selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public string MonthLabel => SelectedMonth.ToString("MMMM yyyy");

    public ObservableCollection<BudgetProgress> CategoryProgress { get; } = new();

    public BudgetViewModel(FinanceDataStore store)
    {
        this.store = store;

        RefreshProgress();

        // Recompute whenever a transaction or category is added, removed or replaced.
        store.Transactions.CollectionChanged += (_, _) => RefreshProgress();
        store.Categories.CollectionChanged += (_, _) =>
        {
            SubscribeToCategoryChanges();
            RefreshProgress();
        };
        SubscribeToCategoryChanges();
    }

    // A category's Name/MonthlyLimit can still change after creation (INotifyPropertyChanged on
    // BudgetCategory), so re-subscribe whenever the set of categories changes, same pattern used
    // in ReportsViewModel.
    private void SubscribeToCategoryChanges()
    {
        foreach (var category in store.Categories)
        {
            category.PropertyChanged -= OnCategoryPropertyChanged;
            category.PropertyChanged += OnCategoryPropertyChanged;
        }
    }

    private void OnCategoryPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshProgress();

    // Rebuilds one BudgetProgress row per category for SelectedMonth.
    private void RefreshProgress()
    {
        CategoryProgress.Clear();
        foreach (var category in store.Categories)
        {
            var spent = store.GetSpentAmount(category, SelectedMonth);
            CategoryProgress.Add(new BudgetProgress { Category = category, AmountSpent = spent });
        }
    }

    // Generated hook: runs after SelectedMonth changes (the < / > buttons), so the list
    // immediately shows the new month's totals.
    partial void OnSelectedMonthChanged(DateTime value) => RefreshProgress();

    [RelayCommand]
    private void PreviousMonth() => SelectedMonth = SelectedMonth.AddMonths(-1);

    [RelayCommand]
    private void NextMonth() => SelectedMonth = SelectedMonth.AddMonths(1);

    // Validates the name/limit fields, adds the category to the shared store, then clears
    // the inputs. The CollectionChanged subscription above refreshes the list.
    [RelayCommand]
    private async Task AddCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            await ShowAlertAsync("Missing Name", "Please enter a category name.");
            return;
        }

        // Requirement: reject a limit of zero or less — a $0 or negative budget isn't meaningful.
        if (!decimal.TryParse(NewCategoryLimitText, out var limit) || limit <= 0)
        {
            await ShowAlertAsync("Invalid Limit", "Please enter a monthly limit greater than zero.");
            return;
        }

        if (store.Categories.Any(c => string.Equals(c.Name, NewCategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            await ShowAlertAsync("Duplicate Category", "A category with that name already exists.");
            return;
        }

        store.Categories.Add(new BudgetCategory(NewCategoryName.Trim(), limit));

        NewCategoryName = string.Empty;
        NewCategoryLimitText = string.Empty;
    }

    /// <summary>
    /// Bound to the "Edit Limit" swipe action on each budget row. Lets the user change an
    /// existing category's monthly limit (the Budget Setup page requirement: "users can specify
    /// limits"). Changing BudgetCategory.MonthlyLimit raises PropertyChanged, which this
    /// ViewModel and ReportsViewModel already listen to, so the progress bar and the
    /// "Budget vs. Actual" chart both update automatically.
    /// </summary>
    [RelayCommand]
    private async Task EditLimit(BudgetProgress progress)
    {
        var input = await ShowPromptAsync(
            $"Edit {progress.Name} Limit",
            "New monthly limit:",
            progress.MonthlyLimit.ToString("0.##"));

        // null = user pressed Cancel.
        if (input is null)
        {
            return;
        }

        if (!decimal.TryParse(input, out var limit) || limit <= 0)
        {
            await ShowAlertAsync("Invalid Limit", "Please enter a monthly limit greater than zero.");
            return;
        }

        progress.Category.MonthlyLimit = limit;
    }

    /// <summary>
    /// Requirement: don't leave transactions pointing at a deleted category. Rather than silently
    /// orphaning them, block the deletion and tell the user to deal with those transactions first.
    /// </summary>
    [RelayCommand]
    private async Task DeleteCategory(BudgetProgress progress)
    {
        if (store.CategoryHasTransactions(progress.Category))
        {
            await ShowAlertAsync(
                "Can't Delete Category",
                $"\"{progress.Name}\" still has transactions linked to it. Delete or reassign those transactions first.");
            return;
        }

        store.Categories.Remove(progress.Category);
    }
}
