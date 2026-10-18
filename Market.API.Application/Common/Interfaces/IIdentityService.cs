using Market.Shared.Contracts.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.API.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<(bool Succeeded, string? UserId, IEnumerable<string> Errors)> CreateUserAsync(
            string fullName, string username, string email, string password, Guid? storeId);

        Task<(bool Succeeded, AuthResult? Result)> LoginAsync(string emailOrUsername, string password);

        Task<(bool Succeeded, AuthResult? Result)> RefreshTokenAsync(string refreshToken);

        Task<(bool Succeeded, UserProfileResponse? Profile, string? Error)> GetUserProfileAsync(Guid userId);

        Task<(bool Succeeded, UserProfileResponse? Profile, string? Error)> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request);
    }
    public class AuthResult
    {
        public string Token { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = default!;
        public DateTime RefreshTokenExpiresAt { get; set; }
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public IList<string> Roles { get; set; } = new List<string>();
        public string? ImageUrl { get; set; }
        public Guid UserId { get; set; }
    }
}
