using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Organization")]
        public string Organization { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // Role selected by admin; keep simple for mockup.
        [Required]
        public string Role { get; set; }
    }

    // ViewModel for success view (shows temp password)
    public class CreateUserSuccessViewModel
    {
        public string FullName { get; set; }
        public string Organization { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string TempPassword { get; set; }
    }
}