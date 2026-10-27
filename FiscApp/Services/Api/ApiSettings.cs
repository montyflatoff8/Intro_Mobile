namespace FiscApp.Services.Api;

/// <summary>
/// Where the FiscApi lives. One place, so no page or service ever hard-codes a URL.
/// The port matches the "https" profile in FiscApi's Properties/launchSettings.json.
/// </summary>
public static class ApiSettings
{
    // Inside the Android emulator, "localhost" means the emulator itself. 10.0.2.2 is the
    // emulator's special alias for the host PC's localhost, where the API is running.
    private const string AndroidEmulatorBase = "https://10.0.2.2:7008/";
    private const string LocalhostBase = "https://localhost:7008/";

    // The trailing slash matters: relative URLs like "api/transactions" are appended to it.
    // Without it, HttpClient would drop the last path segment of the base address.
    public static Uri BaseAddress =>
        new(DeviceInfo.Platform == DevicePlatform.Android ? AndroidEmulatorBase : LocalhostBase);

    // Short on purpose: a stopped API should show "Can't reach the API" quickly, not spin for 100s.
    public static TimeSpan Timeout => TimeSpan.FromSeconds(15);
}
