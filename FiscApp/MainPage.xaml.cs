using System.Collections.ObjectModel;
using FiscApp.Models;

namespace FiscApp;

using FiscApp.Pages;
using FiscApp.Services;

/// <summary>
/// The app's Home page: shows savings goals, lets the user log/edit/delete transactions,
/// and picks which budget category an expense belongs to. All the actual data lives in the
/// shared FinanceDataStore (injected via constructor), not on this page — Transactions,
/// Goals, and Categories below are just read-only pass-throughs so the XAML has something
/// to bind to via BindingContext = this.
/// </summary>
public partial class MainPage : ContentPage
{
    // Tracks which transaction (if any) is currently being edited. Null means the form is in
    // "add a new transaction" mode; non-null means "Save Changes" should update this existing
    // transaction instead of creating a new one. Set in OnEditTransactionClicked, cleared once
    // the edit is saved (or if the transaction being edited gets deleted mid-edit).
    private Transaction? editingTransaction;

    private readonly FinanceDataStore store;

    public MainPage(FinanceDataStore store)
    {
        this.store = store;
        InitializeComponent();
        BindingContext = this;
    }

    public ObservableCollection<Transaction> Transactions => store.Transactions;
    public ObservableCollection<FinancialGoal> Goals => store.Goals;
    public ObservableCollection<BudgetCategory> Categories => store.Categories;

    /// <summary>
    /// Handles both "Add Transaction" and "Save Changes" (the button's Text changes depending
    /// on whether editingTransaction is set). Besides creating/updating the Transaction itself,
    /// this is also responsible for keeping the linked BudgetCategory's AmountSpent correct:
    ///  - Adding a new expense increases its category's AmountSpent.
    ///  - Editing an expense first reverses the OLD amount off the OLD category, then applies
    ///    the NEW amount to the NEW category (which might be the same one, or a different one
    ///    if the user changed the category picker).
    /// Income transactions never touch a category's AmountSpent, since budgets only track spending.
    /// </summary>
    private void OnAddTransactionClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescriptionEntry.Text))
            return;

        if (!decimal.TryParse(AmountEntry.Text, out var amount) || amount <= 0)
            return;

        var type = TypePicker.SelectedIndex == 1
            ? TransactionType.Income
            : TransactionType.Expense;

        var selectedCategory = CategoryPicker.SelectedItem as BudgetCategory;
        var notes = string.IsNullOrWhiteSpace(NotesEntry.Text) ? null : NotesEntry.Text.Trim();

        if (editingTransaction is not null)
        {
            // Reverse the old transaction's effect on its old category (if it had one) before
            // applying the edited values — otherwise the category's AmountSpent would double-count.
            if (editingTransaction.Type == TransactionType.Expense && editingTransaction.Category is not null)
            {
                editingTransaction.Category.AmountSpent -= editingTransaction.Amount;
            }

            var index = Transactions.IndexOf(editingTransaction);
            var updated = new Transaction
            {
                Description = DescriptionEntry.Text.Trim(),
                Amount = amount,
                Type = type,
                Date = editingTransaction.Date,
                Notes = notes,
                Category = type == TransactionType.Expense ? selectedCategory : null
            };
            Transactions[index] = updated;

            // Apply the new (post-edit) amount to whichever category is now selected.
            if (updated.Type == TransactionType.Expense && updated.Category is not null)
            {
                updated.Category.AmountSpent += updated.Amount;
            }

            editingTransaction = null;
            AddOrSaveButton.Text = "Add Transaction";
        }
        else
        {
            var transaction = new Transaction
            {
                Description = DescriptionEntry.Text.Trim(),
                Amount = amount,
                Type = type,
                Date = DateTime.Now,
                Notes = notes,
                Category = type == TransactionType.Expense ? selectedCategory : null
            };

            Transactions.Insert(0, transaction);

            if (transaction.Type == TransactionType.Expense && transaction.Category is not null)
            {
                transaction.Category.AmountSpent += transaction.Amount;
            }
        }

        // Reset the form for the next entry.
        DescriptionEntry.Text = string.Empty;
        AmountEntry.Text = string.Empty;
        NotesEntry.Text = string.Empty;
        TypePicker.SelectedIndex = -1;
        CategoryPicker.SelectedIndex = -1;
    }

    /// <summary>
    /// Fired by the "Edit" swipe action on a transaction row. Populates the form with that
    /// transaction's existing values and switches AddOrSaveButton into "Save Changes" mode.
    /// The actual update happens back in OnAddTransactionClicked once the user submits the form.
    /// </summary>
    private void OnEditTransactionClicked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is Transaction transaction)
        {
            editingTransaction = transaction;
            DescriptionEntry.Text = transaction.Description;
            AmountEntry.Text = transaction.Amount.ToString();
            TypePicker.SelectedIndex = transaction.Type == TransactionType.Income ? 1 : 0;
            CategoryPicker.SelectedItem = transaction.Category;
            NotesEntry.Text = transaction.Notes;
            AddOrSaveButton.Text = "Save Changes";
        }
    }

    /// <summary>
    /// Fired by the "Delete" swipe action. Reverses the transaction's effect on its budget
    /// category (if it was an expense with one) before removing it, so deleting an entry can't
    /// leave a category's AmountSpent permanently overstated. Also resets the edit form if the
    /// transaction being deleted was the one currently being edited.
    /// </summary>
    private void OnDeleteTransactionClicked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is Transaction transaction)
        {
            if (transaction.Type == TransactionType.Expense && transaction.Category is not null)
            {
                transaction.Category.AmountSpent -= transaction.Amount;
            }

            Transactions.Remove(transaction);

            if (editingTransaction == transaction)
            {
                editingTransaction = null;
                AddOrSaveButton.Text = "Add Transaction";
                DescriptionEntry.Text = string.Empty;
                AmountEntry.Text = string.Empty;
                TypePicker.SelectedIndex = -1;
                CategoryPicker.SelectedIndex = -1;
            }
        }
    }

    // Test/scratch navigation button — not part of the core app functionality.
    private async void OnGoToThrowawayClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ThrowawayPage(store));
    }
}
