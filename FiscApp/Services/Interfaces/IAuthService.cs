using FiscApp.Services.Api;

namespace FiscApp.Services.Interfaces;

/// <summary>Logging in and out. ViewModels use this; they never touch the token directly.</summary>
public interface IAuthService
{
    // 200 = logged in (token saved). 401 = wrong username or password.
    // Throws HttpRequestException / OperationCanceledException if the API can't be reached.
    Task<ApiResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    Task LogoutAsync();

    Task<bool> IsLoggedInAsync();
}
