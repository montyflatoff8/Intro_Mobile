using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FiscApp.Models;
using FiscApp.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace FiscApp.Pages;

/// <summary>
/// Shows three charts (via the LiveCharts2 library) built from the shared FinanceDataStore:
///   1. TrendSeries/TrendXAxes   — a line chart of total expense amounts per day.
///   2. BudgetVsActualSeries/BudgetXAxes — a grouped bar chart comparing each category's
///      MonthlyLimit against its AmountSpent.
///   3. CategoryBreakdownSeries — a pie chart showing what proportion of total spending
///      came from each category (categories with $0 spent are excluded so the legend
///      doesn't get cluttered with empty slices).
///
/// All three are recomputed from scratch by RefreshCharts() rather than updated in place —
/// simpler to reason about, at the cost of redoing the LINQ work on every change. The
/// constructor wires up three subscriptions so this happens automatically:
///   - Transactions.CollectionChanged: fires when a transaction is added or removed.
///   - Categories.CollectionChanged: fires when a category is added or removed (and also
///     re-subscribes to the new category's PropertyChanged, see below).
///   - Each individual category's PropertyChanged: fires when AmountSpent changes on an
///     EXISTING category — which is what actually happens most of the time (logging an
///     expense doesn't add/remove a category, it just changes a number on one).
/// </summary>
public partial class Reports : ContentPage, INotifyPropertyChanged
{
    private readonly FinanceDataStore store;

    private ISeries[] trendSeries = Array.Empty<ISeries>();
    private Axis[] trendXAxes = Array.Empty<Axis>();
    private ISeries[] budgetVsActualSeries = Array.Empty<ISeries>();
    private Axis[] budgetXAxes = Array.Empty<Axis>();
    private ISeries[] categoryBreakdownSeries = Array.Empty<ISeries>();

    public ISeries[] CategoryBreakdownSeries
    {
        get => categoryBreakdownSeries;
        set { categoryBreakdownSeries = value; OnPropertyChanged(); }
    }

    // "new" because ContentPage's base BindableObject already exposes its own PropertyChanged-like
    // mechanism internally; declaring our own explicit INotifyPropertyChanged event here (as this
    // page's own property-change notification for TrendSeries/BudgetVsActualSeries/etc.) shadows it
    // rather than conflicting with it.
    public new event PropertyChangedEventHandler? PropertyChanged;

    public Reports(FinanceDataStore store)
    {
        this.store = store;
        InitializeComponent();
        BindingContext = this;

        // Compute the charts once immediately so they're populated the first time this page loads.
        RefreshCharts();

        store.Transactions.CollectionChanged += (s, e) => RefreshCharts();
        store.Categories.CollectionChanged += (s, e) =>
        {
            // A category was added or removed — make sure our per-category subscriptions
            // (below) are up to date for the new set of categories, then recompute.
            SubscribeToCategoryChanges();
            RefreshCharts();
        };
        SubscribeToCategoryChanges();
    }

    // Subscribes this page to every current category's PropertyChanged event, so that changing
    // AmountSpent on an existing category (e.g. from logging an expense on MainPage) triggers a
    // chart refresh here too. Unsubscribing first avoids double-subscribing the same category
    // if this method gets called more than once for it.
    private void SubscribeToCategoryChanges()
    {
        foreach (var category in store.Categories)
        {
            category.PropertyChanged -= OnCategoryPropertyChanged;
            category.PropertyChanged += OnCategoryPropertyChanged;
        }
    }

    private void OnCategoryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BudgetCategory.AmountSpent))
        {
            RefreshCharts();
        }
    }

    public ISeries[] TrendSeries
    {
        get => trendSeries;
        set { trendSeries = value; OnPropertyChanged(); }
    }

    public Axis[] TrendXAxes
    {
        get => trendXAxes;
        set { trendXAxes = value; OnPropertyChanged(); }
    }

    public ISeries[] BudgetVsActualSeries
    {
        get => budgetVsActualSeries;
        set { budgetVsActualSeries = value; OnPropertyChanged(); }
    }

    public Axis[] BudgetXAxes
    {
        get => budgetXAxes;
        set { budgetXAxes = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Rebuilds all four chart-data properties from the current state of the shared store.
    /// Called once at startup and again every time a subscription above detects a relevant change.
    /// </summary>
    private void RefreshCharts()
    {
        // --- Spending trend (line chart): total expenses per calendar day, oldest to newest. ---
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

        // --- Budget vs. actual (grouped bar chart): one "Budget" bar and one "Spent" bar per category. ---
        var categories = store.Categories.ToList();

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
                Values = categories.Select(c => (double)c.AmountSpent).ToArray()
            }
        };

        BudgetXAxes = new Axis[]
        {
            new Axis { Labels = categories.Select(c => c.Name).ToArray() }
        };

        // --- Category breakdown (pie chart): one slice per category that has spending > 0. ---
        // Each PieSeries<double> here represents exactly one slice/category, unlike the line/bar
        // charts above where one series covers all categories at once.
        CategoryBreakdownSeries = categories
            .Where(c => c.AmountSpent > 0)
            .Select(c => new PieSeries<double>
            {
                Name = c.Name,
                Values = new[] { (double)c.AmountSpent }
            })
            .ToArray();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
