using Microsoft.Maui.Graphics;

namespace FiscApp.Services;

/// <summary>
/// DYNAMIC RESOURCES — time-of-day theme (assignment requirement).
///
/// Decides whether it is currently "day" or "night" and writes the matching colors into
/// Application.Current.Resources under the same keys declared in Resources/Styles/Colors.xaml
/// (PageBackgroundColor, CardBackgroundColor, PrimaryTextColor, SecondaryTextColor,
/// DividerColor). Every XAML element that uses {DynamicResource ...} for those keys updates
/// instantly when they are overwritten — no page reload needed.
///
/// Things that can't use {DynamicResource} (the LiveCharts charts on the Reports page are drawn
/// with SkiaSharp paints created in C#) subscribe to <see cref="ThemeChanged"/> instead and
/// repaint themselves.
///
/// Registered as a singleton in MauiProgram.cs and started from App.CreateWindow.
/// </summary>
public class ThemeService
{
    // "Night" is 8:00 pm through 5:59 am.
    private const int NightStartHour = 20;
    private const int NightEndHour = 6;

    private IDispatcherTimer? timer;

    /// <summary>True while the night (dark, low-glare) palette is active.</summary>
    public bool IsNight { get; private set; }

    /// <summary>Raised after the palette flips between day and night.</summary>
    public event EventHandler? ThemeChanged;

    // Has Apply() run at least once? Used so the very first call always writes the colors.
    private bool initialized;

    /// <summary>
    /// Applies the correct palette now, then re-checks every 5 minutes so the theme flips live
    /// if the app is left open across the 6 am / 8 pm boundary.
    /// </summary>
    public void Start(IDispatcher dispatcher)
    {
        Apply();

        timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMinutes(5);
        timer.Tick += (_, _) => Apply();
        timer.Start();
    }

    /// <summary>Checks the clock and swaps the dynamic colors if day/night has changed.</summary>
    public void Apply()
    {
        var hour = DateTime.Now.Hour;
        var isNight = hour >= NightStartHour || hour < NightEndHour;

        // Nothing to do if we're still in the same period as the last check.
        if (initialized && isNight == IsNight)
        {
            return;
        }

        initialized = true;
        IsNight = isNight;

        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        if (isNight)
        {
            // Dark, low-contrast palette to reduce eye strain at night.
            resources["PageBackgroundColor"] = Color.FromArgb("#14161C");
            resources["CardBackgroundColor"] = Color.FromArgb("#1F222B");
            resources["PrimaryTextColor"] = Color.FromArgb("#F0F0F0");
            resources["SecondaryTextColor"] = Color.FromArgb("#A0A0A0");
            resources["DividerColor"] = Color.FromArgb("#2E323D");
        }
        else
        {
            // Bright palette for daytime use (same values as the defaults in Colors.xaml).
            resources["PageBackgroundColor"] = Color.FromArgb("#F5F6FA");
            resources["CardBackgroundColor"] = Colors.White;
            resources["PrimaryTextColor"] = Color.FromArgb("#1F1F1F");
            resources["SecondaryTextColor"] = Color.FromArgb("#6E6E6E");
            resources["DividerColor"] = Color.FromArgb("#E0E0E0");
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Looks up a color from the app's resource dictionaries (static or dynamic) by key.
    /// Lets C# code — models, ViewModels, chart paints — use the same palette as the XAML
    /// instead of hardcoding hex values. Falls back to <paramref name="fallback"/> if the key
    /// isn't found (e.g. in unit tests where there is no running Application).
    /// </summary>
    public static Color GetColor(string key, Color fallback)
    {
        if (Application.Current?.Resources is { } resources
            && resources.TryGetValue(key, out var value)
            && value is Color color)
        {
            return color;
        }

        return fallback;
    }
}
