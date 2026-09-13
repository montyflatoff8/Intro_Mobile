using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace FiscApp.Models
{
    /// <summary>
    /// Represents a single spending category the user has budgeted for (e.g. "Groceries",
    /// with a $400 monthly limit). Tracks how much has been spent against that limit and
    /// exposes computed properties so the UI can show live progress bars and color-coded
    /// warnings without any extra calculation logic in the views.
    /// </summary>
    public class BudgetCategory : INotifyPropertyChanged
    {
        private string name;

        private decimal monthlyLimit;

        private decimal amountSpent;

        // Required by INotifyPropertyChanged. XAML bindings subscribe to this event so that
        // when a property here changes, any bound control (Label, ProgressBar, etc.) refreshes
        // itself automatically instead of the UI going stale.
        public event PropertyChangedEventHandler? PropertyChanged;

        public BudgetCategory(string name, decimal monthlyLimit, decimal amountSpent)
        {
            this.name = name;
            this.monthlyLimit = monthlyLimit;
            this.amountSpent = amountSpent;
        }

        /// <summary>
        /// Color-coded status for this category's spending, used to drive a ProgressBar's
        /// ProgressColor (or any other visual indicator) in the UI.
        /// Green: under 80% of the budget used. Orange: 80-99% used ("approaching limit").
        /// Red: 100% or more used (over budget).
        /// </summary>
        public Color StatusColor
        {
            get
            {
                if (PercentageUsed >= 1.0)
                    return Colors.Red;
                else if (PercentageUsed >= 0.8)
                    return Colors.Orange;
                else
                    return Colors.Green;
            }
        }

        /// <summary>
        /// How much of the monthly limit has been used, expressed as a fraction from 0.0 to 1.0+
        /// (e.g. 0.5 = half the budget spent). This is what a ProgressBar's Progress property binds to.
        /// Guards against dividing by zero if a category is ever created with a $0 limit.
        /// </summary>
        public double PercentageUsed
        {
            get
            {
                return MonthlyLimit == 0 ? 0 : (double)(AmountSpent / MonthlyLimit);
            }
        }

        /// <summary>
        /// Running total of what's been spent in this category this month. Transactions update
        /// this directly (e.g. category.AmountSpent += amount) when an expense is logged, edited,
        /// or removed. Because PercentageUsed and StatusColor are both derived from this value,
        /// changing it also has to notify those two properties so bound charts/progress bars
        /// update in the same instant — that's what NotifyDependentProperties() below does.
        /// </summary>
        public decimal AmountSpent
        {
            get
            {
                return this.amountSpent;
            }
            set
            {
                this.amountSpent = value;
                OnPropertyChanged();
                NotifyDependentProperties();
            }
        }

        /// <summary>
        /// The monthly spending limit the user set for this category. Also affects
        /// PercentageUsed/StatusColor, so changing it re-notifies those as well.
        /// </summary>
        public decimal MonthlyLimit
        {
            get
            {
                return this.monthlyLimit;
            }
            set
            {
                this.monthlyLimit = value;
                OnPropertyChanged();
                NotifyDependentProperties();
            }
        }

        /// <summary>Display name for this category, e.g. "Groceries" or "Entertainment".</summary>
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name =  value;
                OnPropertyChanged();
            }
        }

        // PercentageUsed and StatusColor aren't backed by their own fields — they're calculated
        // from AmountSpent and MonthlyLimit every time they're read. That means the binding system
        // has no way to know they've changed unless we explicitly tell it here, right after the
        // values they depend on change.
        private void NotifyDependentProperties()
        {
            OnPropertyChanged(nameof(PercentageUsed));
            OnPropertyChanged(nameof(StatusColor));
        }

        // Helper that raises PropertyChanged for the calling property. [CallerMemberName] means
        // callers can just write OnPropertyChanged() with no argument from inside a property setter,
        // and the compiler automatically fills in propertyName with that property's own name.
        protected void OnPropertyChanged ([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
