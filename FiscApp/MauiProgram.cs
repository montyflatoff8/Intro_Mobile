using CommunityToolkit.Maui;
using FiscApp.Pages;
using FiscApp.Services;
using FiscApp.Services.Api;
using FiscApp.Services.Auth;
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

            // API + AUTH
            // Token store: singleton so every client sees the same logged-in state.
            builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
            // DelegatingHandlers must be transient: the HttpClient factory builds a new pipeline for each client.
            builder.Services.AddTransient<AuthTokenHandler>();
            // Typed HttpClient for login. Deliberately NO AuthTokenHandler here (never send an old token to login).
            ConfigureApiClient(builder.Services.AddHttpClient<IAuthService, AuthService>());
            // Typed HttpClient for api/transactions, WITH AuthTokenHandler: every request gets the Bearer header.
            ConfigureApiClient(builder.Services.AddHttpClient<ITransactionApiClient, TransactionApiClient>())
                .AddHttpMessageHandler<AuthTokenHandler>();

            // PAGES + VIEWMODELS — transient: a new instance each time one is requested.
            // Shell's ContentTemplate resolves pages through this container; transient guarantees
            // it never receives a page that's already attached to another parent.
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<Budget>();
            builder.Services.AddTransient<BudgetViewModel>();
            builder.Services.AddTransient<Reports>();
            builder.Services.AddTransient<ReportsViewModel>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        // Shared setup for every HttpClient that talks to FiscApi: base address, short timeout,
        // and (Debug only) the handler that trusts the API's localhost dev certificate on Android.
        private static IHttpClientBuilder ConfigureApiClient(IHttpClientBuilder client)
        {
            client.ConfigureHttpClient(http =>
            {
                http.BaseAddress = ApiSettings.BaseAddress;
                http.Timeout = ApiSettings.Timeout;
            });
#if DEBUG
            client.ConfigurePrimaryHttpMessageHandler(DevCertificateHandler.Create);
#endif
            return client;
        }
    }
}