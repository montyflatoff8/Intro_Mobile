using FiscApp.Events;
using FiscApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FiscApp.Services.Interfaces
{
    /// <summary>
    /// Contract for the app's shared data. ViewModels depend on this interface instead of the
    /// concrete FinanceDataStore, so the storage can be swapped (e.g. a database) or faked in tests
    /// without changing any ViewModel.
    /// </summary>
    public interface IFinanceDataStore
    {
        // Live collections the pages bind to.
        ObservableCollection<FinancialGoal> Goals { get; }
        ObservableCollection<Transaction> Transactions { get; }
        ObservableCollection<BudgetCategory> Categories { get; }

        // Custom events raised by the swipe/long-press gestures.
        event EventHandler<TransactionDeletedEvent>? TransactionDeleted;
        event EventHandler<TransactionEditRequestedEvent>? TransactionEditRequested;

        // Budget spent for a category in a given month, calculated from Transactions.
        decimal GetSpentAmount(BudgetCategory category, DateTime month);

        // True if any transaction still uses this category (blocks deleting it).
        bool CategoryHasTransactions(BudgetCategory category);

        void RaiseTransactionDeleted(Transaction transaction);
        void RaiseTransactionEditRequested(Transaction transaction);
        void RemoveTransaction(Transaction transaction);
    }
}
