using Market.Client.API.Interface.User;
using Market.Shared.Contracts.User;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Market.Client.API.Service.User
{
    public class UserApiClient : IUserApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<UserApiClient> _logger;

        public UserApiClient(HttpClient http, ILogger<UserApiClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<(bool Succeeded, UserProfileResponse? Result, string? Error)> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var response = await _http.GetAsync($"api/user/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to get user profile. Status: {StatusCode}, Body: {Body}", 
                        response.StatusCode, body);
                    return (false, null, "Failed to retrieve user profile.");
                }

                var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();
                var result = NormalizeImageUrl(payload?.Data);
                return (true, result, null);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Request timeout while retrieving user profile for userId: {UserId}", userId);
                return (false, null, "The request timed out. Please try again or contact support if the issue persists.");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Request to retrieve user profile was canceled for userId: {UserId}", userId);
                return (false, null, "The request was canceled.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error retrieving user profile for userId: {UserId}", userId);
                return (false, null, "Unable to connect to the server. Please check your connection and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile for userId: {UserId}", userId);
                return (false, null, "An error occurred while retrieving the user profile.");
            }
        }

        public async Task<(bool Succeeded, UserProfileResponse? Result, string? Error)> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/user/{userId}", request);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to update user profile. Status: {StatusCode}, Body: {Body}", 
                        response.StatusCode, body);
                    return (false, null, "Failed to update user profile.");
                }

                var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();
                var result = NormalizeImageUrl(payload?.Data);
                return (true, result, null);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Request timeout while updating user profile for userId: {UserId}", userId);
                return (false, null, "The request timed out. Please try again or contact support if the issue persists.");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Request to update user profile was canceled for userId: {UserId}", userId);
                return (false, null, "The request was canceled.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error updating user profile for userId: {UserId}", userId);
                return (false, null, "Unable to connect to the server. Please check your connection and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for userId: {UserId}", userId);
                return (false, null, "An error occurred while updating the user profile.");
            }
        }

        private UserProfileResponse? NormalizeImageUrl(UserProfileResponse? profile)
        {
            if (profile is null || string.IsNullOrWhiteSpace(profile.ImageUrl))
            {
                return profile;
            }

            if (profile.ImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                profile.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                profile.ImageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }

            if (_http.BaseAddress is null)
            {
                return profile;
            }

            var absoluteUrl = new Uri(_http.BaseAddress, profile.ImageUrl).ToString();
            return profile with { ImageUrl = absoluteUrl };
        }
    }

    internal sealed class ApiResponse<T>
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public IEnumerable<string> Errors { get; init; } = Array.Empty<string>();
    }
}
