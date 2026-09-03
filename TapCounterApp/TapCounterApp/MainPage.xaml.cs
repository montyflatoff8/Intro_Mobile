using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace TapCounterApp
{
    public partial class MainPage : ContentPage
    {

        private int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            count++;
            CounterBtn.Text = $"Clicked {count} times";

            if (count % 10 == 0)
            {
                string clickMessage = $"Congrats, you tapped {count} times!";
                var toast = Toast.Make(clickMessage, ToastDuration.Short);
                MilestoneDisplay.Text = ($"{clickMessage}");
                await toast.Show();
            }
        }
    }
}
