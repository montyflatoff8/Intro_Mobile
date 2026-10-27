using System.Net.Http.Headers;
using FiscApp.Services.Interfaces;

namespace FiscApp.Services.Auth;

/// <summary>
/// Sits in the HttpClient pipeline of the transactions API client and adds
/// "Authorization: Bearer &lt;token&gt;" to every outgoing request when we have a token.
/// Same idea as setting Bearer auth once on the whole Postman collection: no API method
/// has to remember to attach the token itself.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;

    public AuthTokenHandler(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // No token (logged out or expired) = send without the header. GETs still work;
        // writes come back 401 and the app sends the user to the login page.
        var token = await _tokenStore.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Pass the request on to the next handler (eventually the network).
        return await base.SendAsync(request, cancellationToken);
    }
}
