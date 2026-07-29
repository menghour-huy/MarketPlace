using Market.Shared.Contracts.User;

namespace Market.Client.API.Interface.User
{
    public interface IUserApiClient
    {
        Task<(bool Succeeded, UserProfileResponse? Result, string? Error)> GetUserProfileAsync(Guid userId);
        Task<(bool Succeeded, UserProfileResponse? Result, string? Error)> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request);
    }
}
