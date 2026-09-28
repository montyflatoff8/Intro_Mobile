using FiscApp.Models;

namespace FiscApp.Events;

// ─────────────────────────────────────────────────────────────────────────────────────────────
//  CUSTOM EVENTS (assignment requirement: "TransactionDeletedEvent" and
//  "TransactionEditRequestedEvent").
//
//  How the pieces fit together:
//
//    Gesture (View)            →  Command (MainViewModel)          →  Event (FinanceDataStore)
//    swipe left → "Delete"        DeleteTransactionCommand             TransactionDeleted
//    long press → "Delete"        LongPressTransactionCommand          TransactionDeleted
//    long press → "Edit"          LongPressTransactionCommand          TransactionEditRequested
//
//    Event (FinanceDataStore)  →  Handler (MainViewModel)
//    TransactionDeleted           OnTransactionDeleted: removes the transaction from the model
//    TransactionEditRequested     OnTransactionEditRequested: opens the edit form
//
//  The gesture never modifies data itself — it only *raises* the event. The ViewModel is the
//  subscriber that reacts to it, which is exactly the flow the assignment describes
//  ("This event informs the ViewModel to remove the transaction from the model").
//  Because the events live on the shared FinanceDataStore singleton, any other part of the app
//  (e.g. a future "undo" snackbar or an analytics logger) can subscribe to them too.
// ─────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// TransactionDeletedEvent — raised when the user swipes left on a transaction and taps
/// "Delete" (or picks "Delete" from the long-press menu). Carries the transaction to remove.
/// MainViewModel subscribes to FinanceDataStore.TransactionDeleted and removes it from the model.
/// </summary>
public class TransactionDeletedEvent : EventArgs
{
    public required Transaction Transaction { get; init; }
}

/// <summary>
/// TransactionEditRequestedEvent — raised when the user long-presses a transaction and picks
/// "Edit" from the pop-up menu. MainViewModel subscribes to
/// FinanceDataStore.TransactionEditRequested and reacts by loading the transaction into the
/// add/edit form (the "quick edit" option).
/// </summary>
public class TransactionEditRequestedEvent : EventArgs
{
    public required Transaction Transaction { get; init; }
}
