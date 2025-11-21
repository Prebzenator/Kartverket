using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
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

        // Role selected by admin; keep simple for mockup.
        [Required]
        public string Role { get; set; } = string.Empty;
    }

    // ViewModel for success view (shows temp password)
    public class CreateUserSuccessViewModel
    {
        public string FullName { get; set; } = string.Empty; 
        public string Organization { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string TempPassword { get; set; } = string.Empty;
    }
}