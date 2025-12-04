using System.ComponentModel.DataAnnotations;

/// <summary>
/// ViewModel used for handling user login, including email, password,
/// optional persistent login, and an optional return URL after authentication.
/// </summary>
namespace WebApplication1.ViewModels
{
// ViewModel for user login
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

        
// The URL to redirect to after a successful login.
       
        public string? ReturnUrl { get; set; }
    }
}