using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace FiscApp.Models
{
    public class BudgetCategory : INotifyPropertyChanged
    {
        private string name;

        private decimal monthlyLimit;

        private decimal amountSpent;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BudgetCategory(string name, decimal monthlyLimit, decimal amountSpent)
        {
            this.name = name;
            this.monthlyLimit = monthlyLimit;
            this.amountSpent = amountSpent;
        }

        public System.Drawing.Color StatusColor
        {
            get
            {
                if (PercentageUsed >= 1.0)
                {
                    return System.Drawing.Color.Red;
                }
                else if(PercentageUsed >= 0.8)
                {
                    return System.Drawing.Color.Orange;
                }
                else
                {
                    return System.Drawing.Color.Green;
                }
            }
        }

        public double PercentageUsed
        {
            get
            {
                return MonthlyLimit == 0 ? 0 : (double)(AmountSpent / MonthlyLimit);
            }
        }


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

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.Name =  value;
                OnPropertyChanged();
            }
        }

        private void NotifyDependentProperties()
        {
            OnPropertyChanged(nameof(PercentageUsed));
            OnPropertyChanged(nameof(StatusColor));
        }


        protected void OnPropertyChanged ([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
