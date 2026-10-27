using FiscApp.ViewModels;

namespace FiscApp.Pages;

/// <summary>
/// Login screen. All logic is in LoginViewModel; errors are shown inline on the page,
/// so there are no dialog events to wire up here.
/// </summary>
public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
