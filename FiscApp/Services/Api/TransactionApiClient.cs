using System.Net.Http.Json;
using FiscApp.Models.Api;
using FiscApp.Services.Interfaces;

namespace FiscApp.Services.Api;

/// <summary>
/// Typed HttpClient for api/transactions. Its HttpClient pipeline includes AuthTokenHandler
/// (see MauiProgram), so no method here ever touches the token - the handler adds it.
/// This class only knows URLs, JSON, and status codes; what to DO with a 401 or 409 is the
/// ViewModel's decision.
/// </summary>
public class TransactionApiClient : ITransactionApiClient
{
    private const string Route = "api/transactions";   // relative: joined onto ApiSettings.BaseAddress

    private readonly HttpClient _http;

    public TransactionApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<IReadOnlyList<TransactionDto>>> GetAllAsync(
        string? type = null, string? category = null, CancellationToken cancellationToken = default)
    {
        // Only add filters that have a value, and URL-encode them ("Eating Out" -> "Eating%20Out").
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type.Trim())}");
        if (!string.IsNullOrWhiteSpace(category)) query.Add($"category={Uri.EscapeDataString(category.Trim())}");
        var url = query.Count == 0 ? Route : $"{Route}?{string.Join("&", query)}";

        using var response = await _http.GetAsync(url, cancellationToken);
        return await ReadAsync<IReadOnlyList<TransactionDto>>(response, cancellationToken);
    }

    public async Task<ApiResult<TransactionDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"{Route}/{id}", cancellationToken);
        return await ReadAsync<TransactionDto>(response, cancellationToken);
    }

    public async Task<ApiResult<TransactionDto>> CreateAsync(TransactionWriteDto transaction, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(Route, transaction, ApiJson.Options, cancellationToken);
        return await ReadAsync<TransactionDto>(response, cancellationToken);
    }

    public async Task<ApiResult> UpdateAsync(int id, TransactionWriteDto transaction, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"{Route}/{id}", transaction, ApiJson.Options, cancellationToken);
        return await ReadNoContentAsync(response, cancellationToken);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"{Route}/{id}", cancellationToken);
        return await ReadNoContentAsync(response, cancellationToken);
    }

    // ---- Turning an HTTP response into an ApiResult (one place, so every call behaves the same) ----

    // Success with a body (200/201): deserialize it. Failure: read the problem details, if any.
    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<T>.Failure((int)response.StatusCode, await ApiJson.ReadProblemAsync(response, cancellationToken));
        }

        var value = await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, cancellationToken);
        return ApiResult<T>.Success((int)response.StatusCode, value!);
    }

    // Success with no body (204 from PUT/DELETE).
    private static async Task<ApiResult> ReadNoContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return response.IsSuccessStatusCode
            ? ApiResult.Success((int)response.StatusCode)
            : ApiResult.Failure((int)response.StatusCode, await ApiJson.ReadProblemAsync(response, cancellationToken));
    }
}
