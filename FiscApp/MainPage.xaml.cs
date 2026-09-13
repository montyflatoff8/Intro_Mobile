using System.Collections.ObjectModel;
using FiscApp.Models;

namespace FiscApp;

using FiscApp.Pages;
using FiscApp.Services;

public partial class MainPage : ContentPage
{
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


    private void OnAddTransactionClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"NotesEntry.Text = '{NotesEntry.Text}'");

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

        DescriptionEntry.Text = string.Empty;
        AmountEntry.Text = string.Empty;
        NotesEntry.Text = string.Empty;
        TypePicker.SelectedIndex = -1;
        CategoryPicker.SelectedIndex = -1;
    }

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

    private async void OnGoToThrowawayClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ThrowawayPage(store));
    }
}