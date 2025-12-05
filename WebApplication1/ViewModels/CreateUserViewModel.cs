using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    /// <summary>
    /// ViewModel containing the data required to create a new user.
    /// Used in the admin create user form.
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
        // Role assigned to the new user (e.g. Pilot, Registry Administrator, System Administrator)
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel used after successful user creation.
    /// Contains the temporary password and basic user details for display.
    /// </summary>
    public class CreateUserSuccessViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // Temporary password generated for the new user
        public string TempPassword { get; set; } = string.Empty;
    }
}
