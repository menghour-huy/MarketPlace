using System;
using System.Collections.Generic;
using System.Text;
using Market.Shared.Contracts.Auth;

namespace Market.Client.API.Interface.Auth
{
    public interface IAuthApiClient
    {
        Task<(bool Succeeded, AuthResponse? Result, string? Error)> LoginAsync(LoginRequest request);
        Task<(bool Succeeded, Guid? UserId, string? Error)> RegisterAsync(RegisterRequest request);
    }
}
