using System.Net.Http.Json;
using FiscApp.Models.Api;
using FiscApp.Services.Api;
using FiscApp.Services.Interfaces;

namespace FiscApp.Services.Auth;

/// <summary>
/// Calls POST api/auth/login and saves the token. Gets its OWN HttpClient (see MauiProgram)
/// WITHOUT AuthTokenHandler, so an old/expired token is never sent along with a login.
/// </summary>
public class AuthService : IAuthService
{
    // Treat the token as expired 30s early, so it can't run out halfway through a request.
    private static readonly TimeSpan ExpirySafetyMargin = TimeSpan.FromSeconds(30);

    private readonly HttpClient _http;
    private readonly ITokenStore _tokenStore;

    public AuthService(HttpClient http, ITokenStore tokenStore)
    {
        _http = http;
        _tokenStore = tokenStore;
    }

    public async Task<ApiResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var request = new LoginRequestDto(username.Trim(), password);
        using var response = await _http.PostAsJsonAsync("api/auth/login", request, ApiJson.Options, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // For login, 401 means "wrong username or password" - NOT "your session expired".
            return ApiResult.Failure((int)response.StatusCode, await ApiJson.ReadProblemAsync(response, cancellationToken));
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>(ApiJson.Options, cancellationToken)
            ?? throw new InvalidOperationException("Login succeeded but the response body was empty.");

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(login.ExpiresIn) - ExpirySafetyMargin;
        await _tokenStore.SaveAsync(login.AccessToken, expiresAt);

        return ApiResult.Success((int)response.StatusCode);
    }

    // There's no logout endpoint: JWTs aren't stored on the server, so "logging out" just means
    // the app forgets its token. (The token itself stays valid until it expires.)
    public Task LogoutAsync() => _tokenStore.ClearAsync();

    public async Task<bool> IsLoggedInAsync() => await _tokenStore.GetTokenAsync() is not null;
}
