using System.ComponentModel.DataAnnotations;


namespace Market.Client.Models.Auth
{
    public class RegisterViewModel
    {
        [Required, StringLength(200)]
        public string FullName { get; set; } = default!;

        [Required, StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = default!;

        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Required, DataType(DataType.Password), MinLength(8)]
        public string Password { get; set; } = default!;

        [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = default!;
    }
}
