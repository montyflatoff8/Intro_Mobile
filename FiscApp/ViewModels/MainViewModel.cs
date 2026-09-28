using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FiscApp.Events;
using FiscApp.Models;
using FiscApp.Services;

namespace FiscApp.ViewModels;

/// <summary>
/// Backs MainPage (the Transaction List page). Owns every form field the "add/edit transaction"
/// UI needs as a bindable property, and every action as a Command — MainPage.xaml.cs itself now
/// contains nothing but InitializeComponent() and wiring the two alert/action-sheet events up to
/// the real DisplayAlert/DisplayActionSheet calls, since a ViewModel shouldn't call those directly.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    private readonly FinanceDataStore store;

    // [ObservableProperty] (from CommunityToolkit.Mvvm) generates a public "Description" property
    // with change notification from this private field — equivalent to writing out a full
    // get/set/OnPropertyChanged block by hand, just without the boilerplate.
    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string amountText = string.Empty;

    [ObservableProperty]
    private string? notes;

    [ObservableProperty]
    private DateTime date = DateTime.Today;

    [ObservableProperty]
    private TransactionType? selectedType;

    [ObservableProperty]
    private BudgetCategory? selectedCategory;

    // Runs automatically when SelectedType changes (generated partial method). Category only
    // applies to expenses, so switching to Income clears any selected category. The category
    // picker itself is always visible in MainPage.xaml; IsExpenseSelected is available for
    // binding but isn't currently used there.
    partial void OnSelectedTypeChanged(TransactionType? value)
    {
        OnPropertyChanged(nameof(IsExpenseSelected));
        if (value != TransactionType.Expense)
        {
            SelectedCategory = null;
        }
    }

    public bool IsExpenseSelected => SelectedType == TransactionType.Expense;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private Transaction? editingTransaction;

    // Non-null EditingTransaction = the form is in "edit" mode rather than "add" mode.
    public bool IsEditing => EditingTransaction is not null;
    public string SaveButtonText => IsEditing ? "Save Changes" : "Add Transaction";

    // Pass-throughs to the shared store so the page binds to the same live collections
    // the Budget and Reports ViewModels watch.
    public ObservableCollection<Transaction> Transactions => store.Transactions;
    public ObservableCollection<FinancialGoal> Goals => store.Goals;
    public ObservableCollection<BudgetCategory> Categories => store.Categories;
    public TransactionType[] TransactionTypes { get; } = Enum.GetValues<TransactionType>();

    public MainViewModel(FinanceDataStore store)
    {
        this.store = store;

        // CUSTOM EVENT SUBSCRIPTIONS (see Events/TransactionEvents.cs).
        // The gestures on MainPage never change data directly — they only RAISE these events on
        // the shared store. These two subscriptions are where the ViewModel actually reacts:
        //   TransactionDeletedEvent       → remove the transaction from the model
        //   TransactionEditRequestedEvent → open the edit form for that transaction
        store.TransactionDeleted += OnTransactionDeleted;
        store.TransactionEditRequested += OnTransactionEditRequested;
    }

    // Handler for TransactionDeletedEvent: this is the ViewModel "being informed" that the user
    // swiped to delete, and removing the transaction from the model in response.
    private void OnTransactionDeleted(object? sender, TransactionDeletedEvent e)
    {
        store.RemoveTransaction(e.Transaction);

        // If the deleted transaction was open in the edit form, clear the form so the user
        // can't "save changes" to something that no longer exists.
        if (EditingTransaction == e.Transaction)
        {
            ResetForm();
        }
    }

    // Handler for TransactionEditRequestedEvent: loads the transaction into the form.
    private void OnTransactionEditRequested(object? sender, TransactionEditRequestedEvent e)
        => PopulateFormForEdit(e.Transaction);

    // Bound to the Add/Save button. Validates the form, then either replaces the transaction
    // being edited (same list position) or inserts a new one at the top, and clears the form.
    [RelayCommand]
    private async Task SaveTransaction()
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            await ShowAlertAsync("Missing Description", "Please describe what this transaction was for.");
            return;
        }

        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
        {
            await ShowAlertAsync("Invalid Amount", "Please enter an amount greater than zero.");
            return;
        }

        // Requirement: an unselected type must NOT silently become an Expense — it has to be
        // an explicit choice.
        if (SelectedType is null)
        {
            await ShowAlertAsync("Missing Type", "Please choose whether this is an Income or an Expense.");
            return;
        }

        // Requirement: an expense can't be saved without a category.
        if (SelectedType == TransactionType.Expense && SelectedCategory is null)
        {
            await ShowAlertAsync("Missing Category", "Please choose a budget category for this expense.");
            return;
        }

        var trimmedNotes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        var category = SelectedType == TransactionType.Expense ? SelectedCategory : null;

        // Note how much simpler this is than the old version: because a category's spending is
        // now CALCULATED from the Transactions collection on demand (FinanceDataStore.GetSpentAmount)
        // instead of stored as a running total, saving a transaction here doesn't need to manually
        // add/subtract anything from a BudgetCategory. Replacing or inserting the Transaction is
        // the whole operation — whatever reads GetSpentAmount afterward will already see the
        // correct number.
        if (EditingTransaction is not null)
        {
            var index = Transactions.IndexOf(EditingTransaction);
            Transactions[index] = new Transaction
            {
                Description = Description.Trim(),
                Amount = amount,
                Type = SelectedType.Value,
                Date = Date,
                Notes = trimmedNotes,
                Category = category
            };
        }
        else
        {
            Transactions.Insert(0, new Transaction
            {
                Description = Description.Trim(),
                Amount = amount,
                Type = SelectedType.Value,
                Date = Date,
                Notes = trimmedNotes,
                Category = category
            });
        }

        ResetForm();
    }

    // Bound to the "Edit" swipe action — populates the form immediately, no confirmation needed
    // since swiping and tapping "Edit" is already an explicit choice.
    [RelayCommand]
    private void EditTransaction(Transaction transaction) => PopulateFormForEdit(transaction);

    // GESTURE: swipe left → "Delete". Bound to the Delete SwipeItem in MainPage.xaml.
    // Raises TransactionDeletedEvent instead of deleting directly; OnTransactionDeleted (above)
    // is what actually removes the transaction when the event arrives.
    [RelayCommand]
    private void DeleteTransaction(Transaction transaction) => store.RaiseTransactionDeleted(transaction);

    /// <summary>
    /// GESTURE: long press on a transaction row (MainPage.xaml's toolkit:TouchBehavior).
    /// Pops up a menu with three options, so extra actions are available without cluttering
    /// each row with buttons:
    ///   • "View Details" – shows every field of the transaction (the "detailed view").
    ///   • "Edit"         – raises TransactionEditRequestedEvent (the "quick edit option").
    ///   • "Delete"       – raises TransactionDeletedEvent (same path as swipe-to-delete).
    /// Edit and Delete go through the store's custom events rather than calling
    /// PopulateFormForEdit / RemoveTransaction directly, so the gesture is decoupled from what
    /// "edit" or "delete" actually does.
    /// </summary>
    [RelayCommand]
    private async Task LongPressTransaction(Transaction transaction)
    {
        const string viewDetails = "View Details";
        const string edit = "Edit";
        const string delete = "Delete";

        var choice = await ShowActionSheetAsync(
            $"{transaction.Description} ({transaction.DisplayAmount})",
            "Cancel",
            delete,
            new[] { viewDetails, edit });

        switch (choice)
        {
            case viewDetails:
                await ShowAlertAsync("Transaction Details", BuildDetailsText(transaction), "Close");
                break;
            case edit:
                store.RaiseTransactionEditRequested(transaction);
                break;
            case delete:
                store.RaiseTransactionDeleted(transaction);
                break;
        }
    }

    // Builds the multi-line body of the "View Details" pop-up.
    private static string BuildDetailsText(Transaction t) =>
        $"Description: {t.Description}\n" +
        $"Amount: {t.DisplayAmount}\n" +
        $"Type: {t.Type}\n" +
        $"Category: {t.CategoryDisplay}\n" +
        $"Date: {t.Date:dddd, MMMM d, yyyy}\n" +
        $"Notes: {(string.IsNullOrWhiteSpace(t.Notes) ? "(none)" : t.Notes)}";

    // Copies a transaction's values into the form and switches it into edit mode.
    private void PopulateFormForEdit(Transaction transaction)
    {
        EditingTransaction = transaction;
        Description = transaction.Description;
        AmountText = transaction.Amount.ToString();
        Notes = transaction.Notes;
        Date = transaction.Date;
        SelectedType = transaction.Type;
        SelectedCategory = transaction.Category;
    }

    // Clears every field and leaves edit mode (button text goes back to "Add Transaction").
    private void ResetForm()
    {
        EditingTransaction = null;
        Description = string.Empty;
        AmountText = string.Empty;
        Notes = null;
        Date = DateTime.Today;
        SelectedType = null;
        SelectedCategory = null;
    }
}
