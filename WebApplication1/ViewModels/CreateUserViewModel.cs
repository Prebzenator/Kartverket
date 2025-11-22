using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    /// <summary>
    /// Data required to create a new user.
    /// </summary>
    public class CreateUserViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Organization")]
        public string Organization { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data displayed upon successful user creation, including the temporary password.
    /// </summary>
    public class CreateUserSuccessViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string TempPassword { get; set; } = string.Empty;
    }
}