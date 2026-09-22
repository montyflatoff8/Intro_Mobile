using Microsoft.Maui.Graphics;

namespace FiscApp
{
    public partial class App : Application
    {
        // "Night" is 8pm through 5:59am. Pages reference PageBackgroundColor/CardBackgroundColor/
        // PrimaryTextColor/SecondaryTextColor with {DynamicResource ...} (see Colors.xaml) so that
        // overwriting these entries in Application.Current.Resources — which is exactly what
        // ApplyTimeOfDayTheme does — is picked up live, without any page needing to reload.
        private const int NightStartHour = 20;
        private const int NightEndHour = 6;

        private bool? isCurrentlyNight;
        private IDispatcherTimer? themeTimer;

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            ApplyTimeOfDayTheme();

            // Re-check every few minutes so the theme actually flips live if the app is left open
            // across the day/night boundary, rather than only ever being checked once at launch.
            themeTimer = Dispatcher.CreateTimer();
            themeTimer.Interval = TimeSpan.FromMinutes(5);
            themeTimer.Tick += (_, _) => ApplyTimeOfDayTheme();
            themeTimer.Start();

            return window;
        }

        private void ApplyTimeOfDayTheme()
        {
            var hour = DateTime.Now.Hour;
            var isNight = hour >= NightStartHour || hour < NightEndHour;

            if (isCurrentlyNight == isNight)
            {
                return;
            }

            isCurrentlyNight = isNight;

            var resources = Application.Current?.Resources;
            if (resources is null)
            {
                return;
            }

            if (isNight)
            {
                resources["PageBackgroundColor"] = Color.FromArgb("#14161C");
                resources["CardBackgroundColor"] = Color.FromArgb("#1F222B");
                resources["PrimaryTextColor"] = Color.FromArgb("#F0F0F0");
                resources["SecondaryTextColor"] = Color.FromArgb("#A0A0A0");
            }
            else
            {
                resources["PageBackgroundColor"] = Color.FromArgb("#F5F6FA");
                resources["CardBackgroundColor"] = Colors.White;
                resources["PrimaryTextColor"] = Color.FromArgb("#1F1F1F");
                resources["SecondaryTextColor"] = Color.FromArgb("#6E6E6E");
            }
        }
    }
}
