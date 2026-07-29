namespace Market.Shared.Contracts.User
{
    public record UserProfile(
        int Id,
        string FullName,
        string PrimaryEmail,
        string? SecondaryEmail,
        string? ImageUrl,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    public record UpdateUserProfileRequest(
        string FullName,
        string PrimaryEmail,
        string? SecondaryEmail,
        string? ImageBase64
    );

    public record UserProfileResponse(
        Guid Id,
        string FullName,
        string PrimaryEmail,
        string? SecondaryEmail,
        string? ImageUrl,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
