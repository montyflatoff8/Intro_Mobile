using System.Collections.ObjectModel;
using FiscApp.Events;
using FiscApp.Models;
using FiscApp.Services.Interfaces;

namespace FiscApp.Services
{
    /// <summary>
    /// Default in-memory implementation of IFinanceDataStore, registered as a singleton so every ViewModel shares the same data.
    /// </summary>
    public class FinanceDataStore : IFinanceDataStore
    {
        public ObservableCollection<FinancialGoal> Goals { get; } = new();
        public ObservableCollection<Transaction> Transactions { get; } = new();
        public ObservableCollection<BudgetCategory> Categories { get; } = new();

        // Custom events (see Events/TransactionEvents.cs). Raised here, in the store, rather than
        // in a page's code-behind, so that ANY future subscriber — not just MainPage — can react
        // to a transaction being deleted or an edit being requested, without depending on how the
        // gesture that triggered it was implemented.
        public event EventHandler<TransactionDeletedEvent>? TransactionDeleted;
        public event EventHandler<TransactionEditRequestedEvent>? TransactionEditRequested;

        public FinanceDataStore()
        {
            SeedSampleData();
        }

        // Fills the store with demo goals, categories and transactions. Data is in-memory only —
        // nothing is saved to disk, so everything resets when the app restarts.
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
        /// Raises TransactionDeletedEvent. Called by the swipe-to-delete gesture (and the
        /// long-press "Delete" option) via MainViewModel. Note this method does NOT remove
        /// anything itself — it only announces that the user asked for a delete. The actual
        /// removal happens in whoever handles the event (MainViewModel.OnTransactionDeleted,
        /// which calls RemoveTransaction below).
        /// </summary>
        public void RaiseTransactionDeleted(Transaction transaction)
        {
            TransactionDeleted?.Invoke(this, new TransactionDeletedEvent { Transaction = transaction });
        }

        /// <summary>
        /// Removes a transaction from the model. Because budget totals are computed on demand
        /// from this collection (see GetSpentAmount) rather than stored on the category,
        /// removing the transaction here is all that's needed — the Budget and Reports
        /// ViewModels are listening to Transactions.CollectionChanged and refresh themselves.
        /// </summary>
        public void RemoveTransaction(Transaction transaction)
        {
            Transactions.Remove(transaction);
        }

        /// <summary>
        /// Raises TransactionEditRequestedEvent. Doesn't change any data itself — it just
        /// announces "the user wants to edit this transaction". MainViewModel subscribes to this
        /// and reacts by populating the add/edit form. Triggered by the long-press menu.
        /// </summary>
        public void RaiseTransactionEditRequested(Transaction transaction)
        {
            TransactionEditRequested?.Invoke(this, new TransactionEditRequestedEvent { Transaction = transaction });
        }
    }
}
