using FiscApp.ViewModels;

namespace FiscApp.Pages;

/// <summary>
/// Lets the user create new budget categories (a name + a monthly spending limit) and shows
/// each existing category's progress FOR THE SELECTED MONTH, with Previous/Next buttons to
/// look at other months. All of that logic now lives in BudgetViewModel — this code-behind
/// only sets the BindingContext and wires the ViewModel's AlertRequested event to the real
/// DisplayAlertAsync call, since a ViewModel isn't allowed to call it directly.
/// </summary>
public partial class Budget : ContentPage
{
    public Budget(BudgetViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.AlertRequested += (title, message, cancel) => DisplayAlertAsync(title, message, cancel);
    }
}
