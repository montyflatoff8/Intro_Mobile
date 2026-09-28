using FiscApp.ViewModels;

namespace FiscApp.Pages;

/// <summary>
/// Lets the user create new budget categories (a name + a monthly spending limit) and shows
/// each existing category's progress FOR THE SELECTED MONTH, with Previous/Next buttons to
/// look at other months. All of that logic now lives in BudgetViewModel — this code-behind
/// only sets the BindingContext and wires the ViewModel's AlertRequested/PromptRequested events
/// to the real DisplayAlertAsync/DisplayPromptAsync calls, since a ViewModel isn't allowed to call it directly.
/// </summary>
public partial class Budget : ContentPage
{
    public Budget(BudgetViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.AlertRequested += (title, message, cancel) => DisplayAlertAsync(title, message, cancel);
        // Text-input dialog for the "Edit Limit" swipe action (numeric keyboard, pre-filled
        // with the current limit).
        viewModel.PromptRequested += (title, message, initialValue) =>
            DisplayPromptAsync(title, message, "Save", "Cancel",
                keyboard: Keyboard.Numeric, initialValue: initialValue);
    }
}
