using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FiscApp.Models;
using FiscApp.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace FiscApp.ViewModels;

/// <summary>
/// Backs the Reports and Insights page. Builds three LiveCharts2 series from the shared
/// FinanceDataStore:
///   1. TrendSeries/TrendXAxes — a line chart of total expense amounts per day (all-time).
///   2. BudgetVsActualSeries/BudgetXAxes — a grouped bar chart comparing each category's
///      MonthlyLimit against what was actually spent in it THIS MONTH (via
///      FinanceDataStore.GetSpentAmount, the same derived calculation BudgetViewModel uses —
///      no more reading a stored AmountSpent that could disagree with the transaction list).
///   3. CategoryBreakdownSeries — a pie chart of this month's spending by category.
/// All three are rebuilt from scratch whenever the transactions or categories change.
/// </summary>
public partial class ReportsViewModel : BaseViewModel
{
    private readonly FinanceDataStore store;
    private readonly DateTime currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private ISeries[] trendSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] trendXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] budgetVsActualSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] budgetXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] categoryBreakdownSeries = Array.Empty<ISeries>();

    public ReportsViewModel(FinanceDataStore store)
    {
        this.store = store;

        RefreshCharts();

        store.Transactions.CollectionChanged += (_, _) => RefreshCharts();
        store.Categories.CollectionChanged += (_, _) =>
        {
            SubscribeToCategoryChanges();
            RefreshCharts();
        };
        SubscribeToCategoryChanges();
    }

    private void SubscribeToCategoryChanges()
    {
        foreach (var category in store.Categories)
        {
            category.PropertyChanged -= OnCategoryPropertyChanged;
            category.PropertyChanged += OnCategoryPropertyChanged;
        }
    }

    private void OnCategoryPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshCharts();

    private void RefreshCharts()
    {
        var grouped = store.Transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Date.Date)
            .OrderBy(g => g.Key)
            .ToList();

        TrendSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Spending",
                Values = grouped.Select(g => (double)g.Sum(t => t.Amount)).ToArray()
            }
        };

        TrendXAxes = new Axis[]
        {
            new Axis { Labels = grouped.Select(g => g.Key.ToString("MMM d")).ToArray() }
        };

        var categories = store.Categories.ToList();
        var spentThisMonth = categories.ToDictionary(c => c, c => store.GetSpentAmount(c, currentMonth));

        BudgetVsActualSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Budget",
                Values = categories.Select(c => (double)c.MonthlyLimit).ToArray()
            },
            new ColumnSeries<double>
            {
                Name = "Spent",
                Values = categories.Select(c => (double)spentThisMonth[c]).ToArray()
            }
        };

        // Long category names used to overlap each other along the bottom of this chart because
        // they were drawn horizontally with no room to breathe. Rotating them and shrinking the
        // font a little gives LiveCharts room to lay each label out diagonally instead of on top
        // of its neighbor, which is the standard fix for a categorical axis with long labels.
        BudgetXAxes = new Axis[]
        {
            new Axis
            {
                Labels = categories.Select(c => c.Name).ToArray(),
                LabelsRotation = 15,
                TextSize = 11,
                ForceStepToMin = true,
                MinStep = 1
            }
        };

        CategoryBreakdownSeries = categories
            .Where(c => spentThisMonth[c] > 0)
            .Select(c => new PieSeries<double>
            {
                Name = c.Name,
                Values = new[] { (double)spentThisMonth[c] }
            })
            .ToArray();
    }
}
