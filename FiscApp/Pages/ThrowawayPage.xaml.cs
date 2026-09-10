using FiscApp.Services;

namespace FiscApp.Pages;

public partial class ThrowawayPage : ContentPage
{
	private readonly FinanceDataStore store;
	public ThrowawayPage(FinanceDataStore store)
	{
		this.store = store;
		InitializeComponent();
		ShowTransactionCount();
		ShowGoalCount();
	}

	public void ShowTransactionCount()
	{
		TransactionCountLabel.Text = $"{store.Transactions.Count}";
	}

    public void ShowGoalCount()
    {
        GoalCountLabel.Text = $"{store.Goals.Count}";
    }
}