using FiscApp.ViewModels;

namespace FiscApp.Pages;

/// <summary>
/// Shows three LiveCharts2 charts (spending trend, category breakdown, budget vs. actual).
/// All the chart-building logic now lives in ReportsViewModel — this code-behind is just
/// InitializeComponent() + BindingContext, matching the same pattern as MainPage and Budget.
/// </summary>
public partial class Reports : ContentPage
{
    public Reports(ReportsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
