using System.Collections.ObjectModel;
using System.ComponentModel;
using FiscApp.Models;
namespace FiscApp.Pages;

public partial class Budget : ContentPage
{
	private ObservableCollection<BudgetCategory> Categories = new ObservableCollection<BudgetCategory>();
	public Budget()
	{
		InitializeComponent();
		BindingContext = this;
		Categories.Add(new BudgetCategory("Entertainment", 200.00m, 0.00m));
        Categories.Add(new BudgetCategory("Food", 400.00m, 0.00m));
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
		Categories.Add(category);

		CategoryNameEntry.Text = string.Empty;
		CategoryLimitEntry.Text = string.Empty;
    }
}