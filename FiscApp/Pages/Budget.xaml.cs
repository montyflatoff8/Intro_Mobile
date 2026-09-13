using System.Collections.ObjectModel;
using System.ComponentModel;
using FiscApp.Models;
using FiscApp.Services;
namespace FiscApp.Pages;

/// <summary>
/// Lets the user create new budget categories (a name + a monthly spending limit) and
/// shows the existing categories with a color-coded progress bar for each (see
/// BudgetCategory.StatusColor/PercentageUsed). Categories themselves live in the shared
/// FinanceDataStore, so anything added or removed here is immediately reflected on
/// MainPage's category picker and the Reports page's charts too.
/// </summary>
public partial class Budget : ContentPage
{
	private readonly FinanceDataStore store;

    // Read-only pass-through so the CollectionView in Budget.xaml has something to bind
    // ItemsSource to. The actual data lives in the shared store, not a collection owned by
    // this page — that's what keeps this page, MainPage, and Reports all in sync.
    public ObservableCollection<BudgetCategory> Categories => store.Categories;

    public Budget(FinanceDataStore store)
	{
		this.store = store;
		InitializeComponent();
		BindingContext = this;
    }

    /// <summary>
    /// Validates the name/limit entered in the form, then creates and adds a new
    /// BudgetCategory to the shared store. New categories always start at $0 spent.
    /// </summary>
	public async void OnAddCategoryClicked(object sender, EventArgs e)
	{
		string name = CategoryNameEntry.Text.ToString();
		bool IsValid = decimal.TryParse(CategoryLimitEntry.Text, out decimal limit);
		 if (!IsValid || string.IsNullOrWhiteSpace(name))
		{
			await DisplayAlertAsync("Invalid Field(s)", "Please fill out the fields correctly", "");
			return;
		}

		BudgetCategory category = new BudgetCategory(name, limit, 0.00m);
		store.Categories.Add(category);

		CategoryNameEntry.Text = string.Empty;
		CategoryLimitEntry.Text = string.Empty;
    }

    /// <summary>
    /// Fired by the "Delete" swipe action on a category row. Note: this doesn't currently
    /// reassign or warn about any transactions that were pointing at this category — if a
    /// transaction's Category still references the deleted BudgetCategory, that reference
    /// just becomes an orphaned object (it won't crash anything, but that transaction will
    /// no longer affect any budget total shown elsewhere).
    /// </summary>
    private void OnDeleteCategoryClicked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is BudgetCategory category)
        {
            store.Categories.Remove(category);
        }
    }
}
