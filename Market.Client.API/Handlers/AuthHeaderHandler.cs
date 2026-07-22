using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Market.Client.API.Handlers;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthHeaderHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (IsAuthenticationEndpoint(request.RequestUri))
        {
            return base.SendAsync(request, cancellationToken);
        }

        string? token = null;
        var session = _httpContextAccessor.HttpContext?.Session;

        if (session is not null && session.TryGetValue("JwtToken", out var tokenBytes))
        {
            token = Encoding.UTF8.GetString(tokenBytes);
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool IsAuthenticationEndpoint(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return false;
        }

        return requestUri.AbsolutePath.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);
    }
}
