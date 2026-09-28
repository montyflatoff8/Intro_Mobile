using FiscApp.Services;

namespace FiscApp
{
    public partial class App : Application
    {
        // Injected by MAUI's DI container (registered in MauiProgram.cs). Owns the
        // time-of-day theme logic — see Services/ThemeService.cs.
        private readonly ThemeService themeService;

        public App(ThemeService themeService)
        {
            this.themeService = themeService;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // DYNAMIC RESOURCES: pick the day or night palette based on the current time, and
            // keep re-checking on a timer so the UI switches live at 6 am / 8 pm.
            themeService.Start(Dispatcher);

            return window;
        }
    }
}
