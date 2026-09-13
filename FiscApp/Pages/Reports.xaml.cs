using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FiscApp.Models;
using FiscApp.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace FiscApp.Pages;

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

    public new event PropertyChangedEventHandler? PropertyChanged;

    public Reports(FinanceDataStore store)
    {
        this.store = store;
        InitializeComponent();
        BindingContext = this;

        RefreshCharts();

        store.Transactions.CollectionChanged += (s, e) => RefreshCharts();
        store.Categories.CollectionChanged += (s, e) =>
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