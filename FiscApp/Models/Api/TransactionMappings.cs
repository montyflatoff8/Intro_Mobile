namespace FiscApp.Models.Api;

/// <summary>
/// Converts between the API's JSON shape (TransactionDto) and the app's Transaction model.
/// Same idea as TransactionMappings.cs in FiscApi: one place owns the translation.
/// </summary>
public static class TransactionMappings
{
    /// <summary>
    /// API -> app. The API stores a category as a plain name ("Groceries"); the app needs the
    /// BudgetCategory OBJECT, because budgets match on that reference. The caller supplies
    /// resolveCategory, which finds (or creates) the local category for a name.
    /// </summary>
    public static Transaction ToModel(this TransactionDto dto, Func<string, BudgetCategory?> resolveCategory)
    {
        var type = Enum.TryParse<TransactionType>(dto.Type, ignoreCase: true, out var parsed)
            ? parsed
            : TransactionType.Expense;

        return new Transaction
        {
            Id = dto.Id,
            Date = dto.Date,
            Amount = dto.Amount,
            Type = type,
            // Income never has a category (the API enforces this too).
            Category = type == TransactionType.Expense && !string.IsNullOrWhiteSpace(dto.Category)
                ? resolveCategory(dto.Category)
                : null,
            Description = dto.Description,
            Notes = dto.Notes
        };
    }

    /// <summary>App -> API body for POST/PUT. Enum becomes its name, the category object becomes its name.</summary>
    public static TransactionWriteDto ToWriteDto(this Transaction transaction) => new(
        transaction.Date,
        transaction.Amount,
        transaction.Type.ToString(),
        transaction.Type == TransactionType.Expense ? transaction.Category?.Name : null,
        transaction.Description,
        transaction.Notes);
}
