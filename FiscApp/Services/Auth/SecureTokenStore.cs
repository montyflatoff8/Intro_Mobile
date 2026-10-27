using System.Globalization;
using FiscApp.Services.Interfaces;

namespace FiscApp.Services.Auth;

/// <summary>
/// Stores the JWT in SecureStorage (Android Keystore / Windows DPAPI - encrypted by the OS),
/// never in Preferences or a plain file. Registered as a SINGLETON so the in-memory copy is shared.
/// </summary>
public class SecureTokenStore : ITokenStore
{
    private const string TokenKey = "fiscapi_access_token";
    private const string ExpiryKey = "fiscapi_access_token_expires_utc";

    // Fallback: SecureStorage can throw (e.g. unpackaged Windows runs, a reset Android keystore).
    // Then the token lives only in memory - you stay logged in until the app closes.
    private string? _memoryToken;
    private DateTimeOffset? _memoryExpiry;

    public async Task SaveAsync(string token, DateTimeOffset expiresAtUtc)
    {
        _memoryToken = token;
        _memoryExpiry = expiresAtUtc;

        try
        {
            await SecureStorage.Default.SetAsync(TokenKey, token);
            // "O" = round-trip format, so it parses back to exactly the same instant.
            await SecureStorage.Default.SetAsync(ExpiryKey, expiresAtUtc.ToString("O", CultureInfo.InvariantCulture));
        }
        catch (Exception)
        {
            // Keep the in-memory copy; the app still works for this session.
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        var token = _memoryToken;
        var expiry = _memoryExpiry;

        if (token is null)
        {
            try
            {
                token = await SecureStorage.Default.GetAsync(TokenKey);
                var expiryText = await SecureStorage.Default.GetAsync(ExpiryKey);
                if (DateTimeOffset.TryParse(expiryText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    expiry = parsed;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Missing, unreadable, or expired all mean the same thing: not logged in.
        if (string.IsNullOrEmpty(token) || expiry is null || expiry <= DateTimeOffset.UtcNow)
        {
            await ClearAsync();
            return null;
        }

        _memoryToken = token;
        _memoryExpiry = expiry;
        return token;
    }

    public Task ClearAsync()
    {
        _memoryToken = null;
        _memoryExpiry = null;

        try
        {
            SecureStorage.Default.Remove(TokenKey);
            SecureStorage.Default.Remove(ExpiryKey);
        }
        catch (Exception)
        {
            // Nothing stored, or storage unavailable - either way we're logged out.
        }

        return Task.CompletedTask;
    }
}
