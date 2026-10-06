using CommunityToolkit.Maui;
using FiscApp.Pages;
using FiscApp.Services;
using FiscApp.Services.Interfaces;
using FiscApp.ViewModels;
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
                // LiveCharts2: SkiaSharp rendering engine + chart controls for XAML.
                .UseSkiaSharp()
                .UseLiveCharts()
                // CommunityToolkit: TouchBehavior (long-press on transaction rows).
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // SERVICES — singletons: one shared instance for the whole app.
            // The store is registered by its interface, so ViewModels depend on IFinanceDataStore
            // and never on the concrete class. It must be a singleton so every page sees the same data.
            builder.Services.AddSingleton<IFinanceDataStore, FinanceDataStore>();
            // One app-wide day/night theme and timer.
            builder.Services.AddSingleton<ThemeService>();

            // PAGES + VIEWMODELS — transient: a new instance each time one is requested.
            // Shell's ContentTemplate resolves pages through this container; transient guarantees
            // it never receives a page that's already attached to another parent.
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<Budget>();
            builder.Services.AddTransient<BudgetViewModel>();
            builder.Services.AddTransient<Reports>();
            builder.Services.AddTransient<ReportsViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}