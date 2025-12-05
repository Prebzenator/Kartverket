using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    /// <summary>
    /// ViewModel used when a user is forced to change their password.
    /// Contains fields for old, new, and confirmation of new password.
    /// </summary>
    public class ForceChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Old password")]
        // The user's current (temporary) password that must be changed
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        // The new password the user wants to set
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        // Confirmation field to ensure the new password matches
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
