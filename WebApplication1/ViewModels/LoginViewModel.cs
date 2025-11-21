using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    // ViewModel used by the Login.cshtml form.
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

        // Optional returnUrl so Login redirects back to the originally requested page.
        public string? ReturnUrl { get; set; }
    }
}
