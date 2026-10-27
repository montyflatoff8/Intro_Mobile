using System.Text.Json;

namespace FiscApp.Services.Api;

/// <summary>
/// The outcome of one API call. A 4xx answer (400, 401, 404, 409) is a normal, expected
/// result - so it comes back as data the ViewModel can check, not as an exception.
/// Only "no answer at all" (API down, timeout) throws, and ViewModels catch that separately.
/// </summary>
public class ApiResult
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }

    // The RFC 7807 problem-details body the API sends with 400/404/409 (null if none).
    public ApiProblem? Problem { get; init; }

    public bool IsValidationError => StatusCode == 400;
    public bool IsUnauthorized => StatusCode == 401;
    public bool IsNotFound => StatusCode == 404;
    public bool IsConflict => StatusCode == 409;

    /// <summary>A message that's safe to show the user for any failure.</summary>
    public string ErrorMessage => StatusCode switch
    {
        400 when Problem?.Errors is { Count: > 0 } errors =>
            string.Join("\n", errors.Values.SelectMany(messages => messages)),
        400 => Problem?.Title ?? "Some of the values aren't valid.",
        401 => "Please log in to make changes.",
        404 => "That transaction no longer exists.",
        409 => Problem?.Detail ?? "That transaction already exists.",
        >= 500 => "The API had a problem. Try again later.",
        _ => Problem?.Title ?? $"Request failed ({StatusCode})."
    };

    public static ApiResult Success(int statusCode) => new() { IsSuccess = true, StatusCode = statusCode };

    public static ApiResult Failure(int statusCode, ApiProblem? problem) =>
        new() { IsSuccess = false, StatusCode = statusCode, Problem = problem };
}

/// <summary>Same as ApiResult, plus the response body on success (e.g. the created transaction).</summary>
public class ApiResult<T> : ApiResult
{
    public T? Value { get; init; }

    public static ApiResult<T> Success(int statusCode, T value) =>
        new() { IsSuccess = true, StatusCode = statusCode, Value = value };

    public static new ApiResult<T> Failure(int statusCode, ApiProblem? problem) =>
        new() { IsSuccess = false, StatusCode = statusCode, Problem = problem };
}

/// <summary>
/// Client copy of the API's problem-details JSON. Errors keys are the API's JSON field names
/// ("description", "amount") because FiscApi uses SystemTextJsonValidationMetadataProvider.
/// </summary>
public class ApiProblem
{
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

/// <summary>Shared JSON settings and helpers so every call reads and writes the same way.</summary>
public static class ApiJson
{
    // Web defaults = camelCase names on the wire + case-insensitive reading, matching the API.
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    // A 401 has an empty body and a 500 may not be JSON, so this returns null instead of throwing.
    public static async Task<ApiProblem?> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(body) ? null : JsonSerializer.Deserialize<ApiProblem>(body, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
