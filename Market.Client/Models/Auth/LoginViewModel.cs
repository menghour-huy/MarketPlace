
using System.ComponentModel.DataAnnotations;

namespace Market.Client.Models.Auth
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = default!;
    }
}
