using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Shared.Contracts.Auth
{
    public record LoginRequest(string Email, string Password);
}
