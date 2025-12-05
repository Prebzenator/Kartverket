using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    /// <summary>
    /// ViewModel for changing a user's password.
    /// Contains fields for old, new, and confirmation of new password.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        // The user's existing password
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        // The new password the user wants to set
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm new password")]
        // Confirmation field to ensure the new password matches
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
