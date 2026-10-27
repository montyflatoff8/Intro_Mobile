using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FiscApp.Services.Interfaces;

namespace FiscApp.ViewModels;

/// <summary>
/// Backs LoginPage. Trades a username + password for a token through IAuthService, then goes
/// back to whatever page sent the user here. IQueryAttributable receives the optional
/// "?message=..." that SendToLoginAsync adds (e.g. "Please log in to make changes.").
/// </summary>
public partial class LoginViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IAuthService auth;

    [ObservableProperty]
#if DEBUG
    // Debug convenience: the demo username is prefilled. The PASSWORD is not - it's a secret that
    // lives only in the API's User Secrets, and anything typed here would end up in Git.
    private string username = "demo";
#else
    private string username = string.Empty;
#endif

    [ObservableProperty]
    private string password = string.Empty;

    // Why the user was sent here (from the query string). Empty when they tapped "Log in" themselves.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string message = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = string.Empty;

    // While a login request is in flight: shows the spinner and disables the button
    // (NotifyCanExecuteChangedFor re-checks CanLogin), so a double-tap can't send two logins.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool isBusy;

    public bool HasMessage => !string.IsNullOrEmpty(Message);
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public LoginViewModel(IAuthService auth)
    {
        this.auth = auth;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Message = query.TryGetValue("message", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private bool CanLogin() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task Login()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Enter your username and password.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await auth.LoginAsync(Username, Password);

            if (result.IsSuccess)
            {
                Password = string.Empty;   // don't keep the password in memory longer than needed
                await Shell.Current.GoToAsync("..");   // back to the page that sent us here
                return;
            }

            // On THIS page, 401 means bad credentials - not "your session expired".
            ErrorMessage = result.StatusCode switch
            {
                401 => "Wrong username or password.",
                429 => "Too many attempts. Wait a minute and try again.",
                _ => result.ErrorMessage
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // No answer at all (API stopped, Docker still starting, 15s timeout).
            ErrorMessage = ApiUnreachableMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task Cancel() => Shell.Current.GoToAsync("..");
}
