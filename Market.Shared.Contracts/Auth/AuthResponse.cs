using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Shared.Contracts.Auth
{
    public record AuthResponse(
        string Token,
        DateTime ExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        string Email,
        string FullName,
        IList<string> Roles,
        string? ImageUrl = null,
        Guid UserId = default);
}
