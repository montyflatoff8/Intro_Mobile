using System.Collections.ObjectModel;
using FiscApp.Events;
using FiscApp.Models;

namespace FiscApp.Services
{
    /// <summary>
    /// Central, shared source of the app's data. Registered as a singleton in MauiProgram.cs,
    /// so every ViewModel that asks for a FinanceDataStore in its constructor receives this
    /// exact same instance — that's what keeps MainViewModel, BudgetViewModel, and
    /// ReportsViewModel all looking at the same Transactions/Categories/Goals.
    /// </summary>
    public class FinanceDataStore
    {
        public ObservableCollection<FinancialGoal> Goals { get; } = new();
        public ObservableCollection<Transaction> Transactions { get; } = new();
        public ObservableCollection<BudgetCategory> Categories { get; } = new();

        // Custom events (see Events/TransactionEvents.cs). Raised here, in the store, rather than
        // in a page's code-behind, so that ANY future subscriber — not just MainPage — can react
        // to a transaction being deleted or an edit being requested, without depending on how the
        // gesture that triggered it was implemented.
        public event EventHandler<TransactionDeletedEventArgs>? TransactionDeleted;
        public event EventHandler<TransactionEditRequestedEventArgs>? TransactionEditRequested;

        public FinanceDataStore()
        {
            SeedSampleData();
        }

        public void SeedSampleData()
        {
            Goals.Add(new FinancialGoal { Name = "Emergency Fund", TargetAmount = 3000, CurrentAmount = 1200 });
            Goals.Add(new FinancialGoal { Name = "Vacation", TargetAmount = 1500, CurrentAmount = 450 });

            var groceries = new BudgetCategory("Groceries", 400.00m);
            var entertainment = new BudgetCategory("Entertainment", 200.00m);
            Categories.Add(groceries);
            Categories.Add(entertainment);

            Transactions.Add(new Transaction { Description = "Groceries", Amount = 62.18m, Type = TransactionType.Expense, Date = DateTime.Now.AddHours(-3), Category = groceries });
            Transactions.Add(new Transaction { Description = "Paycheck", Amount = 950.00m, Type = TransactionType.Income, Date = DateTime.Now.AddDays(-1) });
        }

        /// <summary>
        /// The core "derived, not accumulated" budget calculation: sums every Expense transaction
        /// for the given category that falls within the given month, straight from the live
        /// Transactions collection. Called fresh every time a BudgetProgress or chart needs a
        /// number — there's no cached total anywhere that could drift out of sync.
        /// </summary>
        public decimal GetSpentAmount(BudgetCategory category, DateTime month)
        {
            return Transactions
                .Where(t =>
                    t.Type == TransactionType.Expense &&
                    t.Category == category &&
                    t.Date.Year == month.Year &&
                    t.Date.Month == month.Month)
                .Sum(t => t.Amount);
        }

        /// <summary>
        /// Used before deleting a category: if any transaction (from any month) still points at
        /// it, deleting the category would orphan that transaction's Category reference. Callers
        /// (BudgetViewModel) use this to block the deletion and ask the user to reassign or
        /// remove those transactions first, rather than silently leaving dangling references.
        /// </summary>
        public bool CategoryHasTransactions(BudgetCategory category)
        {
            return Transactions.Any(t => t.Category == category);
        }

        /// <summary>
        /// Removes a transaction and raises TransactionDeleted. Because budget totals are now
        /// computed on demand from this collection (see GetSpentAmount) rather than stored on
        /// the category, removing a transaction here is all that's needed — nothing has to
        /// separately "undo" its effect on a category the way the old accumulator design did.
        /// </summary>
        public void DeleteTransaction(Transaction transaction)
        {
            Transactions.Remove(transaction);
            TransactionDeleted?.Invoke(this, new TransactionDeletedEventArgs { Transaction = transaction });
        }

        /// <summary>
        /// Doesn't change any data itself — just announces "the user wants to edit this
        /// transaction" via the TransactionEditRequested event. MainViewModel subscribes to this
        /// and reacts by populating the add/edit form. Triggered by the long-press action sheet.
        /// </summary>
        public void RequestEditTransaction(Transaction transaction)
        {
            TransactionEditRequested?.Invoke(this, new TransactionEditRequestedEventArgs { Transaction = transaction });
        }
    }
}
