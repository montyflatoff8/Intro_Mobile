using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FiscApp.Models;

namespace FiscApp.Services
{
    public class FinanceDataStore
    {
        public ObservableCollection<FinancialGoal> Goals { get; } = new();
        public ObservableCollection<Transaction> Transactions { get; } = new();

        public ObservableCollection<BudgetCategory> Categories { get; } = new();

        public FinanceDataStore()
        {
            SeedSampleData();
        }

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
