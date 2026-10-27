using FiscApp.Pages;

namespace FiscApp
{
    // Root navigation container. The flyout menu pages are declared in AppShell.xaml.
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Pages that aren't in the flyout but can be navigated TO (pushed on top of the
            // current page). Shell creates them through the DI container, so their constructor
            // parameters (e.g. LoginViewModel) are filled in automatically.
            Routing.RegisterRoute(Routes.Login, typeof(LoginPage));
        }
    }
}
