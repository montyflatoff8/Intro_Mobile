using FiscApp.Pages;
using FiscApp.Services;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace FiscApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                // Required for LiveCharts2: UseSkiaSharp() registers the rendering engine the
                // charts draw with, and UseLiveCharts() registers the chart controls themselves
                // (CartesianChart, PieChart) so they can be used in XAML.
                .UseSkiaSharp()
                .UseLiveCharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Registering pages here (rather than "new"-ing them up directly) lets MAUI's
            // dependency injection container construct them and automatically pass in whatever
            // constructor dependencies they ask for — in this app, that's the shared
            // FinanceDataStore singleton every page needs. AddSingleton means each of these
            // types has exactly one shared instance for the lifetime of the app; Shell's
            // ContentTemplate={DataTemplate ...} bindings in AppShell.xaml resolve pages through
            // this same container.
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<FinanceDataStore>();
            builder.Services.AddSingleton<ThrowawayPage>();
            builder.Services.AddSingleton<Budget>();
            builder.Services.AddSingleton<Reports>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
