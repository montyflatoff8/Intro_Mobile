namespace FiscApp.Services.Interfaces;

/// <summary>Keeps the login token (JWT) between API calls and app restarts.</summary>
public interface ITokenStore
{
    Task SaveAsync(string token, DateTimeOffset expiresAtUtc);

    // Null when there's no token OR it has expired - an expired token means "logged out".
    Task<string?> GetTokenAsync();

    Task ClearAsync();
}
