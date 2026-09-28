using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FiscApp.Models
{
    /// <summary>
    /// A spending category the user has set a monthly limit for (e.g. "Groceries", $400/month).
    /// This class ONLY stores the category's identity and its limit — it does NOT store how much
    /// has been spent. That used to live here as a mutable "AmountSpent" accumulator, but that
    /// design couldn't distinguish this month's spending from last month's, and every transaction
    /// edit/delete had to carefully add/subtract from it by hand (a real source of bugs).
    ///
    /// "How much has been spent against this category, for a given month" is now computed on
    /// demand from the actual Transaction records (see FinanceDataStore.GetSpentAmount and the
    /// BudgetProgress class) rather than stored here. That means spending totals can never drift
    /// out of sync with the transaction list, and reporting on a previous month is just a matter
    /// of asking for a different month — no separate historical storage needed.
    /// </summary>
    public class BudgetCategory : INotifyPropertyChanged
    {
        private string name;
        private decimal monthlyLimit;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BudgetCategory(string name, decimal monthlyLimit)
        {
            this.name = name;
            this.monthlyLimit = monthlyLimit;
        }

        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }

        public decimal MonthlyLimit
        {
            get => monthlyLimit;
            set { monthlyLimit = value; OnPropertyChanged(); }
        }

        // Notifies listeners (BudgetViewModel/ReportsViewModel) so they recompute when a
        // category is renamed or its limit changes.
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
