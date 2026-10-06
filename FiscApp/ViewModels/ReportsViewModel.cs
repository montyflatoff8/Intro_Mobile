using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FiscApp.Models;
using FiscApp.Services;
using FiscApp.Services.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace FiscApp.ViewModels;

/// <summary>
/// Backs the Reports and Insights page. Builds three LiveCharts2 series from the shared
/// FinanceDataStore:
///   1. TrendSeries/TrendXAxes — a line chart of total expense amounts per day (all-time).
///   2. BudgetVsActualSeries/BudgetXAxes — a grouped bar chart comparing each category's
///      MonthlyLimit against what was actually spent in it THIS MONTH (via
///      FinanceDataStore.GetSpentAmount, the same derived calculation BudgetViewModel uses).
///   3. CategoryBreakdownSeries — a pie chart of this month's spending by category.
/// Plus a few plain-text "insights" for the current month (income, expenses, net, top category,
/// categories over budget).
///
/// Everything is rebuilt from scratch whenever the transactions or categories change, AND
/// whenever the day/night theme flips (ThemeService.ThemeChanged). The charts are drawn with
/// SkiaSharp paints created in C#, so unlike XAML controls they can't use {DynamicResource};
/// rebuilding them with fresh colors is how they follow the time-of-day theme.
/// </summary>
public partial class ReportsViewModel : BaseViewModel
{
    private readonly IFinanceDataStore store;
    private readonly DateTime currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private ISeries[] trendSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] trendXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] trendYAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] budgetVsActualSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] budgetXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] budgetYAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] categoryBreakdownSeries = Array.Empty<ISeries>();

    // Legend text paint for all charts — bound in Reports.xaml (LegendTextPaint) so the legend
    // text is readable on both the light (day) and dark (night) backgrounds.
    [ObservableProperty]
    private SolidColorPaint legendTextPaint = new(SKColors.Black);

    // ── Month-at-a-glance insights (bound to the summary card at the top of Reports.xaml) ──
    [ObservableProperty]
    private string monthLabel = string.Empty;

    [ObservableProperty]
    private string incomeText = string.Empty;

    [ObservableProperty]
    private string expensesText = string.Empty;

    [ObservableProperty]
    private string netText = string.Empty;

    [ObservableProperty]
    private string topCategoryText = string.Empty;

    [ObservableProperty]
    private string overBudgetText = string.Empty;

    public ReportsViewModel(IFinanceDataStore store, ThemeService themeService)
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

        // DYNAMIC THEME: repaint the charts with the new day/night colors when the theme flips.
        themeService.ThemeChanged += (_, _) => RefreshCharts();
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

    // Converts a MAUI Color (from the app's resource dictionaries) into the SkiaSharp color
    // LiveCharts draws with.
    private static SKColor ToSk(Color c) =>
        new((byte)(c.Red * 255), (byte)(c.Green * 255), (byte)(c.Blue * 255), (byte)(c.Alpha * 255));

    // Reads a color from Colors.xaml / ThemeService by key and converts it for LiveCharts.
    private static SKColor Resource(string key, Color fallback) => ToSk(ThemeService.GetColor(key, fallback));

    // Rebuilds all chart data. Assigning new arrays to the [ObservableProperty] fields raises
    // PropertyChanged, which is what makes the bound charts redraw.
    private void RefreshCharts()
    {
        // ── Theme colors for this rebuild ─────────────────────────────────────────────────
        // Text/grid colors are DYNAMIC (change day ↔ night); series colors are the STATIC
        // semantic colors, so the charts use the same palette as the buttons and budget bars.
        var textColor = Resource("SecondaryTextColor", Colors.Gray);
        var gridColor = Resource("DividerColor", Colors.LightGray);
        var accent = Resource("AccentColor", Colors.Blue);
        var danger = Resource("DangerColor", Colors.Red);
        var cardBackground = Resource("CardBackgroundColor", Colors.White);

        LegendTextPaint = new SolidColorPaint(Resource("PrimaryTextColor", Colors.Black));

        // Helper: an axis whose labels and grid lines use the current theme colors.
        Axis ThemedAxis(string[]? labels = null) => new()
        {
            Labels = labels,
            LabelsPaint = new SolidColorPaint(textColor),
            SeparatorsPaint = new SolidColorPaint(gridColor) { StrokeThickness = 1 },
            TextSize = 11
        };

        // 1) Spending trend: total expenses per calendar day, oldest to newest.
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
                Values = grouped.Select(g => (double)g.Sum(t => t.Amount)).ToArray(),
                Stroke = new SolidColorPaint(accent) { StrokeThickness = 3 },
                Fill = null,
                GeometryStroke = new SolidColorPaint(accent) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(cardBackground)
            }
        };

        TrendXAxes = new[] { ThemedAxis(grouped.Select(g => g.Key.ToString("MMM d")).ToArray()) };
        TrendYAxes = new[] { ThemedAxis() };

        // 2) Budget vs. actual: compute each category's spend for the current month once,
        //    then reuse it for the bar chart, the pie chart and the insights.
        var categories = store.Categories.ToList();
        var spentThisMonth = categories.ToDictionary(c => c, c => store.GetSpentAmount(c, currentMonth));

        BudgetVsActualSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Budget",
                Values = categories.Select(c => (double)c.MonthlyLimit).ToArray(),
                Fill = new SolidColorPaint(accent)
            },
            new ColumnSeries<double>
            {
                Name = "Spent",
                Values = categories.Select(c => (double)spentThisMonth[c]).ToArray(),
                Fill = new SolidColorPaint(danger)
            }
        };

        // Long category names used to overlap each other along the bottom of this chart.
        // Rotating them and shrinking the font gives LiveCharts room to lay each label out
        // diagonally instead of on top of its neighbor.
        var budgetX = ThemedAxis(categories.Select(c => c.Name).ToArray());
        budgetX.LabelsRotation = 15;
        budgetX.ForceStepToMin = true;
        budgetX.MinStep = 1;
        BudgetXAxes = new[] { budgetX };
        BudgetYAxes = new[] { ThemedAxis() };

        // 3) Category breakdown pie: one slice per category (skipping $0 categories). Slices
        //    cycle through the app's semantic palette instead of LiveCharts' default colors.
        var palette = new[]
        {
            accent,
            Resource("SuccessColor", Colors.Green),
            Resource("WarningColor", Colors.Orange),
            danger,
            Resource("Tertiary", Colors.DarkBlue),
            Resource("SecondaryDarkText", Colors.LightBlue)
        };

        CategoryBreakdownSeries = categories
            .Where(c => spentThisMonth[c] > 0)
            .Select((c, i) => new PieSeries<double>
            {
                Name = c.Name,
                Values = new[] { (double)spentThisMonth[c] },
                Fill = new SolidColorPaint(palette[i % palette.Length])
            })
            .ToArray();

        RefreshInsights(categories, spentThisMonth);
    }

    // Plain-text insights for the current month, shown in the summary card on Reports.xaml.
    private void RefreshInsights(List<BudgetCategory> categories, Dictionary<BudgetCategory, decimal> spentThisMonth)
    {
        var thisMonth = store.Transactions
            .Where(t => t.Date.Year == currentMonth.Year && t.Date.Month == currentMonth.Month)
            .ToList();

        var income = thisMonth.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = thisMonth.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var net = income - expenses;

        MonthLabel = currentMonth.ToString("MMMM yyyy");
        IncomeText = $"Income: {income:C}";
        ExpensesText = $"Expenses: {expenses:C}";
        NetText = net >= 0 ? $"Saved: {net:C}" : $"Overspent: {-net:C}";

        var top = spentThisMonth.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).FirstOrDefault();
        TopCategoryText = top.Key is null
            ? "Top category: no spending yet"
            : $"Top category: {top.Key.Name} ({top.Value:C})";

        var over = categories.Where(c => spentThisMonth[c] > c.MonthlyLimit).Select(c => c.Name).ToList();
        OverBudgetText = over.Count == 0
            ? "All categories are within budget."
            : $"Over budget: {string.Join(", ", over)}";
    }
}
