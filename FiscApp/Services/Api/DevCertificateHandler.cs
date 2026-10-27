#if DEBUG
using System.Net.Security;

namespace FiscApp.Services.Api;

/// <summary>
/// DEBUG ONLY. The API runs HTTPS with the ASP.NET Core development certificate
/// (issued to "CN=localhost"). Your PC trusts it after "dotnet dev-certs https --trust",
/// but the Android emulator doesn't, so every call would fail with a certificate error.
/// This handler accepts that one certificate and validates every other certificate normally.
/// The whole file is inside #if DEBUG, so a Release build contains no trace of it.
/// </summary>
public static class DevCertificateHandler
{
    public static HttpMessageHandler Create()
    {
        var handler = new HttpClientHandler();

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            handler.ServerCertificateCustomValidationCallback = (_, cert, _, errors) =>
                cert?.Issuer == "CN=localhost" || errors == SslPolicyErrors.None;
        }

        return handler;
    }
}
#endif
