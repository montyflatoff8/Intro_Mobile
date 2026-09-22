namespace FiscApp;

using FiscApp.Pages;
using FiscApp.Services;
using FiscApp.ViewModels;

/// <summary>
/// The app's Home page: shows savings goals, lets the user log/edit/delete transactions,
/// and picks which budget category an expense belongs to. All the actual application logic
/// (validation, saving, editing, deleting, the long-press action sheet) now lives in
/// MainViewModel — this code-behind only does two things: set the BindingContext, and wire
/// the ViewModel's AlertRequested/ActionSheetRequested events to the real dialog APIs, since
/// a ViewModel isn't allowed to call Page.DisplayAlert directly.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly FinanceDataStore store;

    public MainPage(MainViewModel viewModel, FinanceDataStore store)
    {
        this.store = store;
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.AlertRequested += (title, message, cancel) => DisplayAlertAsync(title, message, cancel);
        viewModel.ActionSheetRequested += (title, cancel, destruction, buttons) =>
            DisplayActionSheet(title, cancel, destruction, buttons);
    }

    // Test/scratch navigation button — not part of the core app functionality.
    private async void OnGoToThrowawayClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ThrowawayPage(store));
    }
}
