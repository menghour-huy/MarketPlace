using Market.Client.API.Interface.User;
using Market.Client.Models.User;
using Market.Shared.Contracts.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Market.Client.Controllers
{
    [Authorize]
    [Route("user")]
    public class UserController : Controller
    {
        private readonly IUserApiClient _userApiClient;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserApiClient userApiClient, ILogger<UserController> logger)
        {
            _userApiClient = userApiClient;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub");

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            var token = HttpContext.Session.GetString("JwtToken");
            if (TryExtractUserIdFromJwt(token, out userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User ID not found in claims or token");
        }

        private static bool TryExtractUserIdFromJwt(string? token, out Guid userId)
        {
            userId = default;

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var parts = token.Split('.');
            if (parts.Length < 2)
            {
                return false;
            }

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            switch (payload.Length % 4)
            {
                case 2:
                    payload += "==";
                    break;
                case 3:
                    payload += "=";
                    break;
            }

            try
            {
                var jsonBytes = Convert.FromBase64String(payload);
                using var document = JsonDocument.Parse(jsonBytes);

                if (document.RootElement.TryGetProperty("sub", out var subElement) &&
                    Guid.TryParse(subElement.GetString(), out userId))
                {
                    return true;
                }
            }
            catch (FormatException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }

            return false;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> Settings()
        {
            try
            {
                var userId = GetCurrentUserId();
                var (succeeded, userProfile, error) = await _userApiClient.GetUserProfileAsync(userId);

                if (!succeeded || userProfile == null)
                {
                    TempData["ErrorMessage"] = error ?? "Failed to load user profile";
                    return View(new UserSettingsViewModel());
                }

                var viewModel = new UserSettingsViewModel
                {
                    FullName = userProfile.FullName,
                    PrimaryEmail = userProfile.PrimaryEmail,
                    SecondaryEmail = userProfile.SecondaryEmail,
                    ImageUrl = userProfile.ImageUrl,
                    CreatedAt = userProfile.CreatedAt,
                    UpdatedAt = userProfile.UpdatedAt
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user settings");
                TempData["ErrorMessage"] = "An error occurred while loading your settings.";
                return View(new UserSettingsViewModel());
            }
        }

        [HttpPost("settings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(UserSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = GetCurrentUserId();
                string? imageBase64 = model.ImageBase64;

                // Convert uploaded image to base64 if provided
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    if (!IsValidImageFile(model.ImageFile))
                    {
                        ModelState.AddModelError("ImageFile", "Please upload a valid image file (JPG, PNG, GIF)");
                        return View(model);
                    }

                    imageBase64 = await ConvertFileToBase64(model.ImageFile);
                }

                var request = new UpdateUserProfileRequest(
                    model.FullName,
                    model.PrimaryEmail,
                    model.SecondaryEmail,
                    imageBase64
                );

                var (succeeded, updatedProfile, error) = await _userApiClient.UpdateUserProfileAsync(userId, request);

                if (!succeeded || updatedProfile == null)
                {
                    TempData["ErrorMessage"] = error ?? "Failed to update user profile";
                    return View(model);
                }

                // Update session with new full name
                HttpContext.Session.SetString("UserFullName", updatedProfile.FullName);

                TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user settings");
                TempData["ErrorMessage"] = "An error occurred while updating your profile.";
                return View(model);
            }
        }

        private bool IsValidImageFile(IFormFile file)
        {
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
                return false;

            var extension = Path.GetExtension(file.FileName).ToLower();
            return allowedExtensions.Contains(extension);
        }

        private async Task<string> ConvertFileToBase64(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                return Convert.ToBase64String(fileBytes);
            }
        }
    }
}
