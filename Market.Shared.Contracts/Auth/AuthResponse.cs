using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Shared.Contracts.Auth
{
    public record AuthResponse(string Token, DateTime ExpiresAt, string Email, string FullName, IList<string> Roles);
}
