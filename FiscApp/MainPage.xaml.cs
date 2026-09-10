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


    private void OnAddTransactionClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescriptionEntry.Text))
            return;

        if (!decimal.TryParse(AmountEntry.Text, out var amount) || amount <= 0)
            return;

        var type = TypePicker.SelectedIndex == 1
            ? TransactionType.Income
            : TransactionType.Expense;

        if (editingTransaction is not null)
        {
            var index = Transactions.IndexOf(editingTransaction);
            Transactions[index] = new Transaction
            {
                Description = DescriptionEntry.Text.Trim(),
                Amount = amount,
                Type = type,
                Date = editingTransaction.Date
            };
            editingTransaction = null;
            AddOrSaveButton.Text = "Add Transaction";
        }
        else
        {
            Transactions.Insert(0, new Transaction
            {
                Description = DescriptionEntry.Text.Trim(),
                Amount = amount,
                Type = type,
                Date = DateTime.Now
            });
        }

        DescriptionEntry.Text = string.Empty;
        AmountEntry.Text = string.Empty;
        TypePicker.SelectedIndex = -1;
    }

    private void OnEditTransactionClicked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is Transaction transaction)
        {
            editingTransaction = transaction;
            DescriptionEntry.Text = transaction.Description;
            AmountEntry.Text = transaction.Amount.ToString();
            TypePicker.SelectedIndex = transaction.Type == TransactionType.Income ? 1 : 0;
            AddOrSaveButton.Text = "Save Changes";
        }
    }

    private void OnDeleteTransactionClicked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is Transaction transaction)
        {
            Transactions.Remove(transaction);

            // If you were mid-edit on the transaction you just deleted, reset the form.
            if (editingTransaction == transaction)
            {
                editingTransaction = null;
                AddOrSaveButton.Text = "Add Transaction";
                DescriptionEntry.Text = string.Empty;
                AmountEntry.Text = string.Empty;
                TypePicker.SelectedIndex = -1;
            }
        }
    }

    private async void OnGoToThrowawayClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ThrowawayPage(store));
    }
}