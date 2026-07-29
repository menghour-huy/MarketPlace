using System.ComponentModel.DataAnnotations;

namespace Market.Client.Models.User
{
    public class UserSettingsViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name must be less than 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Primary email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string PrimaryEmail { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid secondary email address")]
        public string? SecondaryEmail { get; set; }

        public string? ImageUrl { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ImageFile { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // For image preview
        public string? ImageBase64 { get; set; }
    }
}
