using Market.API.Application.Common.Interfaces;
using Market.Shared.Contracts.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Market.API.Persistence.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            ILogger<IdentityService> logger)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
        }

        public async Task<(bool, string?, IEnumerable<string>)> CreateUserAsync(
    string fullName, string username, string email, string password, Guid? storeId)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return (false, null, new[] { "A user with this email already exists." });
            }

            var baseUsername = NormalizeUsernameCandidate(username, email);
            var availableUsername = await GenerateAvailableUsernameAsync(baseUsername);

            var user = new ApplicationUser
            {
                UserName = availableUsername,
                Email = email,
                FullName = fullName,
                StoreId = storeId
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                return (false, null, result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, storeId.HasValue ? "StoreOwner" : "Customer");

            return (true, user.Id.ToString(), Array.Empty<string>());
        }

        public async Task<(bool, AuthResult?)> LoginAsync(string emailOrUsername, string password)
        {
            var totalStopwatch = Stopwatch.StartNew();

            var findUserStopwatch = Stopwatch.StartNew();
            var user = await ResolveUserByEmailOrUsernameAsync(emailOrUsername);
            findUserStopwatch.Stop();

            if (user is null || !user.IsActive)
            {
                totalStopwatch.Stop();
                _logger.LogInformation(
                    "Login failed for {Identifier}. User lookup took {FindUserMs}ms. Total {TotalMs}ms.",
                    emailOrUsername,
                    findUserStopwatch.ElapsedMilliseconds,
                    totalStopwatch.ElapsedMilliseconds);

                return (false, null);
            }

            var passwordStopwatch = Stopwatch.StartNew();
            var validPassword = await _userManager.CheckPasswordAsync(user, password);
            passwordStopwatch.Stop();

            if (!validPassword)
            {
                totalStopwatch.Stop();
                _logger.LogInformation(
                    "Login failed for {Identifier}. User lookup {FindUserMs}ms, password check {PasswordCheckMs}ms. Total {TotalMs}ms.",
                    emailOrUsername,
                    findUserStopwatch.ElapsedMilliseconds,
                    passwordStopwatch.ElapsedMilliseconds,
                    totalStopwatch.ElapsedMilliseconds);

                return (false, null);
            }

            var rolesStopwatch = Stopwatch.StartNew();
            var roles = await _userManager.GetRolesAsync(user);
            rolesStopwatch.Stop();

            var tokenStopwatch = Stopwatch.StartNew();
            var token = GenerateJwtToken(user, roles);
            tokenStopwatch.Stop();

            totalStopwatch.Stop();
            if (totalStopwatch.ElapsedMilliseconds >= 2000)
            {
                _logger.LogWarning(
                    "Slow login for {Identifier}. User lookup {FindUserMs}ms, password check {PasswordCheckMs}ms, roles {RolesMs}ms, token {TokenMs}ms. Total {TotalMs}ms.",
                    emailOrUsername,
                    findUserStopwatch.ElapsedMilliseconds,
                    passwordStopwatch.ElapsedMilliseconds,
                    rolesStopwatch.ElapsedMilliseconds,
                    tokenStopwatch.ElapsedMilliseconds,
                    totalStopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Login succeeded for {Identifier}. User lookup {FindUserMs}ms, password check {PasswordCheckMs}ms, roles {RolesMs}ms, token {TokenMs}ms. Total {TotalMs}ms.",
                    emailOrUsername,
                    findUserStopwatch.ElapsedMilliseconds,
                    passwordStopwatch.ElapsedMilliseconds,
                    rolesStopwatch.ElapsedMilliseconds,
                    tokenStopwatch.ElapsedMilliseconds,
                    totalStopwatch.ElapsedMilliseconds);
            }

            return (true, new AuthResult
            {
                Token = token.Token,
                ExpiresAt = token.ExpiresAt,
                Email = user.Email!,
                FullName = user.FullName,
                Roles = roles
            });
        }

        private async Task<ApplicationUser?> ResolveUserByEmailOrUsernameAsync(string emailOrUsername)
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername))
            {
                return null;
            }

            var identifier = emailOrUsername.Trim();
            if (identifier.Contains('@'))
            {
                var byEmail = await _userManager.FindByEmailAsync(identifier);
                if (byEmail is not null)
                {
                    return byEmail;
                }
            }

            return await _userManager.FindByNameAsync(identifier);
        }

        private static string NormalizeUsernameCandidate(string? requestedUsername, string email)
        {
            var candidate = string.IsNullOrWhiteSpace(requestedUsername)
                ? email.Split('@')[0]
                : requestedUsername.Trim();

            var sanitized = new string(candidate.Where(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-').ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? ($"user{Guid.NewGuid():N}")[..12] : sanitized;
        }

        private async Task<string> GenerateAvailableUsernameAsync(string baseUsername)
        {
            if (await _userManager.FindByNameAsync(baseUsername) is null)
            {
                return baseUsername;
            }

            var suffix = 1;
            string candidate;
            do
            {
                candidate = $"{baseUsername}{suffix}";
                suffix++;
            } while (await _userManager.FindByNameAsync(candidate) is not null);

            return candidate;
        }

        public async Task<(bool Succeeded, UserProfileResponse? Profile, string? Error)> GetUserProfileAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return (false, null, "User not found.");

            return (true, MapToProfileResponse(user), null);
        }

        public async Task<(bool Succeeded, UserProfileResponse? Profile, string? Error)> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return (false, null, "User not found.");

            // Check if the new primary email is already taken by a different user
            if (!string.Equals(user.Email, request.PrimaryEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(request.PrimaryEmail);
                if (existing != null && existing.Id != userId)
                    return (false, null, "This email address is already in use.");
            }

            user.FullName = request.FullName;
            user.Email = request.PrimaryEmail;
            user.UserName = request.PrimaryEmail;
            user.SecondaryEmail = request.SecondaryEmail;
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.ImageBase64))
            {
                var (saved, imagePath, imageError) = await SaveUserImageAsync(request.ImageBase64, user.ImageUrl);
                if (!saved || string.IsNullOrWhiteSpace(imagePath))
                {
                    return (false, null, imageError ?? "Failed to save profile image.");
                }

                user.ImageUrl = imagePath;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return (false, null, errors);
            }

            return (true, MapToProfileResponse(user), null);
        }

        private static UserProfileResponse MapToProfileResponse(ApplicationUser user) =>
            new(
                user.Id,
                user.FullName,
                user.Email!,
                user.SecondaryEmail,
                user.ImageUrl,
                user.CreatedAt,
                user.UpdatedAt
            );

        private static (bool Success, byte[] Bytes, string Extension) TryDecodeImage(string imageBase64)
        {
            if (string.IsNullOrWhiteSpace(imageBase64))
            {
                return (false, Array.Empty<byte>(), string.Empty);
            }

            var payload = imageBase64.Trim();
            var extension = ".jpg";

            if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var separator = payload.IndexOf(',');
                if (separator <= 0)
                {
                    return (false, Array.Empty<byte>(), string.Empty);
                }

                var metadata = payload[..separator];
                payload = payload[(separator + 1)..];

                if (metadata.Contains("image/png", StringComparison.OrdinalIgnoreCase)) extension = ".png";
                else if (metadata.Contains("image/gif", StringComparison.OrdinalIgnoreCase)) extension = ".gif";
                else if (metadata.Contains("image/webp", StringComparison.OrdinalIgnoreCase)) extension = ".webp";
                else extension = ".jpg";
            }

            try
            {
                var bytes = Convert.FromBase64String(payload);
                return (bytes.Length > 0, bytes, extension);
            }
            catch (FormatException)
            {
                return (false, Array.Empty<byte>(), string.Empty);
            }
        }

        private async Task<(bool Success, string? ImagePath, string? Error)> SaveUserImageAsync(string imageBase64, string? existingImagePath)
        {
            var (decoded, bytes, extension) = TryDecodeImage(imageBase64);
            if (!decoded)
            {
                return (false, null, "Invalid image content.");
            }

            var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "users");
            Directory.CreateDirectory(uploadsDirectory);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsDirectory, fileName);
            await File.WriteAllBytesAsync(fullPath, bytes);

            if (!string.IsNullOrWhiteSpace(existingImagePath) &&
                existingImagePath.StartsWith("/uploads/users/", StringComparison.OrdinalIgnoreCase))
            {
                var existingFileName = Path.GetFileName(existingImagePath);
                var existingPath = Path.Combine(uploadsDirectory, existingFileName);
                if (File.Exists(existingPath))
                {
                    File.Delete(existingPath);
                }
            }

            return (true, $"/uploads/users/{fileName}", null);
        }

        private (string Token, DateTime ExpiresAt) GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email!),
                new(ClaimTypes.Name, user.FullName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (user.StoreId.HasValue)
                claims.Add(new Claim("storeId", user.StoreId.Value.ToString()));

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
