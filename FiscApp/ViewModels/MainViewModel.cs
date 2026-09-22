using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    // Category selection is only meaningful for expenses — this flips the category picker's
    // visibility in MainPage.xaml so the user isn't asked to pick a category for income.
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

    public bool IsEditing => EditingTransaction is not null;
    public string SaveButtonText => IsEditing ? "Save Changes" : "Add Transaction";

    public ObservableCollection<Transaction> Transactions => store.Transactions;
    public ObservableCollection<FinancialGoal> Goals => store.Goals;
    public ObservableCollection<BudgetCategory> Categories => store.Categories;
    public TransactionType[] TransactionTypes { get; } = Enum.GetValues<TransactionType>();

    public MainViewModel(FinanceDataStore store)
    {
        this.store = store;

        // The long-press gesture calls store.RequestEditTransaction(...) rather than editing the
        // form directly (see LongPressTransactionCommand below) — this subscription is what
        // actually reacts to that event and populates the form. Any other part of the app could
        // subscribe to the same event without touching this ViewModel at all.
        store.TransactionEditRequested += (_, e) => PopulateFormForEdit(e.Transaction);
    }

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

    // Bound to the "Delete" swipe action.
    [RelayCommand]
    private void DeleteTransaction(Transaction transaction)
    {
        store.DeleteTransaction(transaction);

        if (EditingTransaction == transaction)
        {
            ResetForm();
        }
    }

    /// <summary>
    /// Bound to a long-press gesture on a transaction row (see MainPage.xaml's TouchBehavior).
    /// Presents an action sheet with "Edit" and "Delete" — this is the "quick edit options" /
    /// "detailed view" affordance the long-press gesture is meant to provide. Choosing either
    /// option goes through the FinanceDataStore's custom events rather than calling
    /// PopulateFormForEdit or DeleteTransaction directly, so the gesture is decoupled from
    /// exactly what "edit" or "delete" does.
    /// </summary>
    [RelayCommand]
    private async Task LongPressTransaction(Transaction transaction)
    {
        var choice = await ShowActionSheetAsync(
            $"{transaction.Description} — {transaction.DisplayAmount}",
            "Cancel",
            "Delete",
            new[] { "Edit" });

        if (choice == "Edit")
        {
            store.RequestEditTransaction(transaction);
        }
        else if (choice == "Delete")
        {
            store.DeleteTransaction(transaction);
        }
    }

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
