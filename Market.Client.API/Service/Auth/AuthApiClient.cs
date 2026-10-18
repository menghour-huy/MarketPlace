using Market.Client.API.Interface.Auth;
using Market.Shared.Contracts.Auth;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Market.Client.API.Auth;

public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AuthApiClient> _logger;

    public AuthApiClient(HttpClient http, ILogger<AuthApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<(bool, AuthResponse?, string?)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, body);
                return (false, null, "Invalid email or password.");
            }

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

            if (payload is null)
            {
                _logger.LogWarning("Login returned an empty response body for {Identifier}", request.EmailOrUsername);
                return (false, null, "Login failed.");
            }

            if (!payload.Success)
            {
                var error = payload.Errors.FirstOrDefault() ?? payload.Message;
                _logger.LogWarning("Login failed for {Identifier}. Message: {Message}", request.EmailOrUsername, error);
                return (false, null, string.IsNullOrWhiteSpace(error) ? "Invalid email or password." : error);
            }

            var result = payload.Data;
            if (result is null || string.IsNullOrWhiteSpace(result.Token))
            {
                _logger.LogWarning("Login returned an invalid auth payload for {Identifier}. Message: {Message}", request.EmailOrUsername, payload.Message);
                return (false, null, "Login failed.");
            }

            result = result with { Roles = result.Roles ?? [] };
            result = NormalizeImageUrl(result);
            return (true, result, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection to Market.API failed during login for {Identifier}", request.EmailOrUsername);
            return (false, null, "Unable to reach the server. Please try again later.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Login request to Market.API timed out for {Identifier}", request.EmailOrUsername);
            return (false, null, "The server took too long to respond.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Login response from Market.API could not be parsed for {Identifier}", request.EmailOrUsername);
            return (false, null, "Login failed.");
        }
    }

    public async Task<(bool, AuthResponse?, string?)> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/refresh", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Refresh token failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, body);
                return (false, null, "Session expired. Please log in again.");
            }

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

            if (payload is null)
            {
                _logger.LogWarning("Refresh token returned an empty response body.");
                return (false, null, "Session expired. Please log in again.");
            }

            if (!payload.Success)
            {
                var error = payload.Errors.FirstOrDefault() ?? payload.Message;
                _logger.LogWarning("Refresh token failed. Message: {Message}", error);
                return (false, null, string.IsNullOrWhiteSpace(error) ? "Session expired. Please log in again." : error);
            }

            var result = payload.Data;
            if (result is null || string.IsNullOrWhiteSpace(result.Token) || string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                _logger.LogWarning("Refresh token returned invalid payload. Message: {Message}", payload.Message);
                return (false, null, "Session expired. Please log in again.");
            }

            result = result with { Roles = result.Roles ?? [] };
            result = NormalizeImageUrl(result);
            return (true, result, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection to Market.API failed during token refresh");
            return (false, null, "Unable to reach the server. Please try again later.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Refresh token request to Market.API timed out");
            return (false, null, "The server took too long to respond.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Refresh token response from Market.API could not be parsed");
            return (false, null, "Session expired. Please log in again.");
        }
    }

    public async Task<(bool, Guid?, string?)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Register failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, body);
                return (false, null, body);
            }

            var result = await response.Content.ReadFromJsonAsync<RegisterResult>();
            return (true, result?.UserId, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection to Market.API failed during registration for {Email}", request.Email);
            return (false, null, "Unable to reach the server. Please try again later.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Register request to Market.API timed out for {Email}", request.Email);
            return (false, null, "The server took too long to respond.");
        }
    }

    private AuthResponse? NormalizeImageUrl(AuthResponse? result)
    {
        if (result is null || string.IsNullOrWhiteSpace(result.ImageUrl))
        {
            return result;
        }

        if (result.ImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            result.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            result.ImageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }

        if (_http.BaseAddress is null)
        {
            return result;
        }

        var absoluteUrl = new Uri(_http.BaseAddress, result.ImageUrl).ToString();
        return result with { ImageUrl = absoluteUrl };
    }
}

internal sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IEnumerable<string> Errors { get; init; } = Array.Empty<string>();
}

internal record RegisterResult(Guid UserId);
