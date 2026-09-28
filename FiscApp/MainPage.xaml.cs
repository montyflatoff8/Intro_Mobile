namespace FiscApp;

using FiscApp.ViewModels;

/// <summary>
/// The app's Home page (Transaction List page): shows savings goals, lets the user log/edit/
/// delete transactions, and picks which budget category an expense belongs to. All the actual
/// application logic (validation, saving, the swipe/long-press gestures and their custom events)
/// lives in MainViewModel — this code-behind only does two things: set the BindingContext, and
/// wire the ViewModel's AlertRequested/ActionSheetRequested events to the real dialog APIs,
/// since a ViewModel isn't allowed to call Page.DisplayAlert directly.
/// </summary>
public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.AlertRequested += (title, message, cancel) => DisplayAlertAsync(title, message, cancel);
        viewModel.ActionSheetRequested += (title, cancel, destruction, buttons) =>
            DisplayActionSheet(title, cancel, destruction, buttons);
    }
}
