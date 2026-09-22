using FiscApp.Models;

namespace FiscApp.Events;

/// <summary>
/// Raised by FinanceDataStore.DeleteTransaction whenever a transaction is removed — whether
/// that removal came from the swipe-to-delete gesture on MainPage or the "Delete" choice in
/// the long-press action sheet. Anything that cares "a transaction just disappeared" (a
/// confirmation toast, an analytics log, a future undo feature) can subscribe to
/// FinanceDataStore.TransactionDeleted instead of that logic living inside the button handler
/// that happened to trigger the delete.
/// </summary>
public class TransactionDeletedEventArgs : EventArgs
{
    public required Transaction Transaction { get; init; }
}

/// <summary>
/// Raised by FinanceDataStore.RequestEditTransaction whenever the user asks to edit a
/// transaction via the long-press action sheet on MainPage. MainViewModel subscribes to
/// FinanceDataStore.TransactionEditRequested and reacts by populating the add/edit form —
/// the long-press gesture itself doesn't touch the form directly, it just raises this event
/// and lets whoever is listening decide what "edit" means.
/// </summary>
public class TransactionEditRequestedEventArgs : EventArgs
{
    public required Transaction Transaction { get; init; }
}
