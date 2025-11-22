using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        /// <summary>
        /// The URL to redirect to after a successful login.
        /// </summary>
        public string? ReturnUrl { get; set; }
    }
}