using FiscApp.Models;
using System.Collections.ObjectModel;


namespace FiscApp;

public partial class MainPage : ContentPage
{
    public ObservableCollection<FinancialGoal> Goals { get; } = new();
    public ObservableCollection<Transaction> Transactions { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        SeedSampleData();
    }

    private void SeedSampleData()
    {
        // Barebones starter data so the screen isn't empty on first run.
        // Wire these up to real storage later.
        Goals.Add(new FinancialGoal { Name = "Emergency Fund", TargetAmount = 3000, CurrentAmount = 1200 });
        Goals.Add(new FinancialGoal { Name = "Vacation", TargetAmount = 1500, CurrentAmount = 450 });

        Transactions.Add(new Transaction { Description = "Groceries", Amount = 62.18m, Type = TransactionType.Expense, Date = DateTime.Now.AddHours(-3) });
        Transactions.Add(new Transaction { Description = "Paycheck", Amount = 950.00m, Type = TransactionType.Income, Date = DateTime.Now.AddDays(-1) });
    }

    private void OnAddTransactionClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescriptionEntry.Text))
            return;

        if (!decimal.TryParse(AmountEntry.Text, out var amount) || amount <= 0)
            return;

        var type = TypePicker.SelectedIndex == 1
            ? TransactionType.Income
            : TransactionType.Expense;

        // Newest first, so the entry you just logged shows up immediately.
        Transactions.Insert(0, new Transaction
        {
            Description = DescriptionEntry.Text.Trim(),
            Amount = amount,
            Type = type,
            Date = DateTime.Now
        });

        DescriptionEntry.Text = string.Empty;
        AmountEntry.Text = string.Empty;
        TypePicker.SelectedIndex = -1;
    }
}
