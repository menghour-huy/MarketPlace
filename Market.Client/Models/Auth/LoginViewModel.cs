
using System.ComponentModel.DataAnnotations;

namespace Market.Client.Models.Auth
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email or Username")]
        public string EmailOrUsername { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = default!;
    }
}
