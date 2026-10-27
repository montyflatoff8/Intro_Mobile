namespace FiscApp.Models.Api;

// Client copies of FiscApi's DTOs/TransactionResponse.cs and DTOs/TransactionRequest.cs.
// They mirror the JSON exactly (plain strings, no enums or objects), and Mappings.cs converts
// them to and from the app's own Transaction model.

/// <summary>What GET/POST return for a transaction.</summary>
public record TransactionDto(
    int Id,
    DateTime CreatedAt,
    DateTime Date,
    decimal Amount,
    string Type,          // "Income" or "Expense"
    string? Category,     // a category NAME on the API, a BudgetCategory object in the app
    string Description,
    string? Notes);

/// <summary>Body for POST and PUT. No Id or CreatedAt: the server owns those.</summary>
public record TransactionWriteDto(
    DateTime Date,
    decimal Amount,
    string Type,
    string? Category,
    string Description,
    string? Notes);
