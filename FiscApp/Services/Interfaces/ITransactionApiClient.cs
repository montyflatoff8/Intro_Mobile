using FiscApp.Models.Api;
using FiscApp.Services.Api;

namespace FiscApp.Services.Interfaces;

/// <summary>
/// Every call the app makes to api/transactions. 4xx answers come back as ApiResult;
/// "API unreachable" throws HttpRequestException (or OperationCanceledException on timeout).
/// </summary>
public interface ITransactionApiClient
{
    // GET api/transactions (public). Both filters optional.
    Task<ApiResult<IReadOnlyList<TransactionDto>>> GetAllAsync(
        string? type = null, string? category = null, CancellationToken cancellationToken = default);

    // GET api/transactions/{id} (public). 404 if missing.
    Task<ApiResult<TransactionDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // POST (needs token). 201 + created transaction, or 400 / 401 / 409.
    Task<ApiResult<TransactionDto>> CreateAsync(TransactionWriteDto transaction, CancellationToken cancellationToken = default);

    // PUT (needs token). 204, or 400 / 401 / 404 / 409. Full replace: send every field.
    Task<ApiResult> UpdateAsync(int id, TransactionWriteDto transaction, CancellationToken cancellationToken = default);

    // DELETE (needs token). 204, or 401 / 404.
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
