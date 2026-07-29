using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Shared.Contracts.Auth
{
    public record RegisterRequest(string FullName, string Username, string Email, string Password, Guid? StoreId);
}
