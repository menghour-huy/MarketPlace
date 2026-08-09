using Market.Shared.Contracts.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Market.Client.API.Handlers;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthHeaderHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpRequestMessage? retryRequest = null;

        if (!IsAuthenticationEndpoint(request.RequestUri) && request.Headers.Authorization is null)
        {
            var token = await ResolveJwtTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            retryRequest = await CloneRequestAsync(request, cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || retryRequest is null)
        {
            return response;
        }

        var refreshedToken = await RefreshAccessTokenAsync(request, cancellationToken);
        if (string.IsNullOrWhiteSpace(refreshedToken))
        {
            return response;
        }

        response.Dispose();
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task<string?> RefreshAccessTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        var refreshToken = httpContext.Session.GetString("RefreshToken");
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        if (request.RequestUri is null)
        {
            return null;
        }

        var baseAddress = request.RequestUri.IsAbsoluteUri
            ? new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority))
            : null;

        if (baseAddress is null)
        {
            return null;
        }

        using var refreshClient = new HttpClient { BaseAddress = baseAddress };
        var refreshResponse = await refreshClient.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest(refreshToken), cancellationToken);

        if (!refreshResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(cancellationToken: cancellationToken);
        var data = payload?.Data;

        if (payload is null || !payload.Success || data is null || string.IsNullOrWhiteSpace(data.Token) || string.IsNullOrWhiteSpace(data.RefreshToken))
        {
            return null;
        }

        httpContext.Session.SetString("JwtToken", data.Token);
        httpContext.Session.SetString("AccessToken", data.Token);
        httpContext.Session.SetString("RefreshToken", data.RefreshToken);

        return data.Token;
    }

    private async Task<string?> ResolveJwtTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        var token = httpContext.Session.GetString("AccessToken") ?? httpContext.Session.GetString("JwtToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            token = await httpContext.GetTokenAsync("access_token");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            token = authResult.Properties?.GetTokenValue("access_token");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            token = httpContext.User.FindFirst("access_token")?.Value;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        token = token.Trim();
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token["Bearer ".Length..].Trim();
        }

        return token;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    private static bool IsAuthenticationEndpoint(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return false;
        }

        var path = requestUri.IsAbsoluteUri ? requestUri.AbsolutePath : requestUri.OriginalString;
        return path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("api/auth", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ApiResponse<T>
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public IEnumerable<string> Errors { get; init; } = Array.Empty<string>();
    }
}
