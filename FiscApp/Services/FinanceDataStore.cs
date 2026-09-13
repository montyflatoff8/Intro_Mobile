using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FiscApp.Models;

namespace FiscApp.Services
{
    /// <summary>
    /// Central, shared source of the app's data. Registered as a singleton in MauiProgram.cs,
    /// so every page that asks for a FinanceDataStore in its constructor (MainPage, Budget,
    /// Reports) receives this exact same instance. That's what lets, for example, a category
    /// created on the Budget page immediately show up in MainPage's category picker, and a
    /// transaction logged on MainPage immediately update the charts on the Reports page —
    /// they're all reading and writing the same three collections below.
    ///
    /// All three collections are ObservableCollection, which raises a CollectionChanged event
    /// whenever an item is added or removed. Combined with each item's own INotifyPropertyChanged
    /// (see BudgetCategory), this is what makes the UI update live without any manual "refresh"
    /// step: the CollectionView/CartesianChart controls are watching these collections directly.
    /// </summary>
    public class FinanceDataStore
    {
        public ObservableCollection<FinancialGoal> Goals { get; } = new();
        public ObservableCollection<Transaction> Transactions { get; } = new();

        public ObservableCollection<BudgetCategory> Categories { get; } = new();

        public FinanceDataStore()
        {
            SeedSampleData();
        }

        // Populates the app with a bit of starter data so the UI isn't empty on first launch.
        // In a real app this would eventually be replaced by loading saved data from local
        // storage or a database instead of hardcoding sample values here.
        public void SeedSampleData()
        {
            Goals.Add(new FinancialGoal { Name = "Emergency Fund", TargetAmount = 3000, CurrentAmount = 1200 });
            Goals.Add(new FinancialGoal { Name = "Vacation", TargetAmount = 1500, CurrentAmount = 450 });

            Transactions.Add(new Transaction { Description = "Groceries", Amount = 62.18m, Type = TransactionType.Expense, Date = DateTime.Now.AddHours(-3) });
            Transactions.Add(new Transaction { Description = "Paycheck", Amount = 950.00m, Type = TransactionType.Income, Date = DateTime.Now.AddDays(-1) });

            Categories.Add(new BudgetCategory("Entertainment", 200.00m, 0.00m));
            Categories.Add(new BudgetCategory("Food", 400.00m, 0.00m));
        }
    }
}
