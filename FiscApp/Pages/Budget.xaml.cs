using System.Collections.ObjectModel;
using System.ComponentModel;
using FiscApp.Models;
using FiscApp.Services;
namespace FiscApp.Pages;

public partial class Budget : ContentPage
{
	private readonly FinanceDataStore store;

    public ObservableCollection<BudgetCategory> Categories => store.Categories;

    public Budget(FinanceDataStore store)
	{
		this.store = store;
		InitializeComponent();
		BindingContext = this;
    }

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

    private void OnDeleteCategoryClicked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is BudgetCategory category)
        {
            store.Categories.Remove(category);
        }
    }
}